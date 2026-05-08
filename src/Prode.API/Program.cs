using System.Reflection;
using System.Text;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prode.Application.Interfaces;
using Prode.Application.Services;
using Prode.Infrastructure.Data;
using Prode.Infrastructure.Data.Seed;
using Prode.Infrastructure.Repositories;
using Prode.Infrastructure.Services;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Prode.Domain.Entities;
using Prode.Application;
using Prode.Application.Helpers;
using Prode.API.Converters;
using Prode.API;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7
    )
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// 🔹 Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Converter GLOBAL: TODAS las fechas se serializan en UTC con formato ISO 8601 + Z
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
        
        // ✅ Restaurar camelCase que usaba la API originalmente
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();

// 🔹 Swagger + Bearer
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Prode API", Version = "v1" });

    // Configurar Bearer
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese 'Bearer {token}'"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
        Array.Empty<string>()
        }
    };

    c.AddSecurityRequirement(securityRequirement);

    // Manejo de IFormFile (sin tocar Type="string")
    c.SupportNonNullableReferenceTypes();
    c.UseAllOfForInheritance(); // opcional, si tenés herencias de modelos

    // Comentarios XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressMapClientErrors = true;
});

// 🔹 Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 32))
    ));

// 🔹 Identity
builder.Services.Configure<IdentityOptions>(options =>
{
    // Configuración de contraseña
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false; // No requerir caracteres especiales
    options.Password.RequiredLength = 8;

    // Configuración de usuario
    options.User.RequireUniqueEmail = true;
});

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 🔹 DI
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();

builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ICityRepository, CityRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IPredictionRepository, PredictionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();

// 🔹 Posts
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();

// 🔹 Maintenance
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddHostedService<MaintenanceBackgroundService>();

// 🔹 Push Notifications
builder.Services.AddScoped<IPushNotificationService, WebPushNotificationService>();
builder.Services.AddScoped<IUserPushSubscriptionRepository, UserPushSubscriptionRepository>();
builder.Services.AddScoped<SendPushNotificationJob>();

// 🔹 Hangfire - Background Job Processing
var hangfireSection = builder.Configuration.GetSection("Hangfire");
builder.Services.AddHangfire(config =>
{
    config.UseStorage(new MySqlStorage(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        new MySqlStorageOptions
        {
            TablesPrefix = "Hangfire_",
            QueuePollInterval = TimeSpan.FromSeconds(hangfireSection.GetValue<int>("SchedulePollingIntervalSeconds", 15))
        }
    ));
});

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = hangfireSection["ServerName"] ?? $"{Environment.MachineName}-prode-api";
    options.WorkerCount = hangfireSection.GetValue<int>("WorkerCount", 5);
    options.Queues = hangfireSection.GetSection("Queues").Get<string[]>() ?? ["default"];
    options.SchedulePollingInterval = TimeSpan.FromSeconds(
        hangfireSection.GetValue<int>("SchedulePollingIntervalSeconds", 15));
});

// 🔹 JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,                        // ahora validamos quién emite el token
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,                      // validamos a quién va dirigido
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,                    // sin tolerancia de expiración

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "")),

            NameClaimType = JwtRegisteredClaimNames.Sub, // sigue usando "sub"
            RoleClaimType = ClaimTypes.Role
        };
    });

// CORS Configurable desde appsettings
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
            .Get<string[]>() 
            ?? new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:5175" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// 🔹 Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prode API V1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Inicializar helper de fechas
var dateTimeLogger = app.Services.GetRequiredService<ILogger<Program>>();
DateTimeHelper.Initialize(dateTimeLogger);

// 🔹 Seed de SuperAdmin y ResultTypes
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // ✅ Aplicar migraciones pendientes automaticamente al iniciar
    await dbContext.Database.MigrateAsync();

    await UserSeed.SeedSuperAdminAsync(userManager, roleManager);
    await ResultTypeSeed.SeedResultTypesAsync(dbContext);
}

app.UseAuthentication();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var hangfireTables = new[] { "Hangfire_Job", "Hangfire_State", "Hangfire_Hash", "Hangfire_List", "Hangfire_Set", "Hangfire_Counter", "Hangfire_AggregatedCounter", "Hangfire_Server" };

    foreach (var table in hangfireTables)
    {
        try
        {
            var exists = await dbContext.Database.ExecuteSqlRawAsync(
                $"SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{{0}}'",
                table);

            if (exists > 0)
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    $"ALTER TABLE `{table}` CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
                logger.LogInformation("✅ [Hangfire] Tabla {Table} migrada a utf8mb4", table);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ [Hangfire] No se pudo migrar tabla {Table}", table);
        }
    }
}

app.UseAuthentication();
app.UseAuthorization();

// 🔹 Hangfire Dashboard (protegido por autenticación en producción)
if (hangfireSection.GetSection("Dashboard").GetValue<bool>("Enabled"))
{
    var dashboardOptions = new DashboardOptions
    {
        DashboardTitle = "Prode Hangfire",
        AppPath = "/hangfire",
        DisplayStorageConnectionString = false,
        StatsPollingInterval = 5000
    };

    // En producción, requerir autenticación y rol Admin
    if (!app.Environment.IsDevelopment() || hangfireSection.GetSection("Dashboard").GetValue<bool>("RequireAuthentication"))
    {
        dashboardOptions.Authorization = new[]
        {
            new Prode.API.HangfireAuthorizationFilter()
        };
    }

    app.UseHangfireDashboard(
        hangfireSection["Dashboard:Url"] ?? "/hangfire",
        dashboardOptions
    );
}

app.MapControllers();

app.Run();