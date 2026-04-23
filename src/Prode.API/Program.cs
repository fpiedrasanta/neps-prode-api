using System.Reflection;
using AspNetCoreRateLimit;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
using Prode.Application.Helpers;
using Prode.API.Converters;

var builder = WebApplication.CreateBuilder(args);

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

// 🔹 Rate Limiting Protection
builder.Services.AddMemoryCache();
builder.Services.Configure<AspNetCoreRateLimit.IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<AspNetCoreRateLimit.RateLimitRule>
    {
        // ✅ Proteccion global: max 100 requests por minuto por IP
        new AspNetCoreRateLimit.RateLimitRule 
        { 
            Endpoint = "*", 
            Period = "1m", 
            Limit = 100 
        },
        
        // ✅ Proteccion especial login: max 5 intentos por minuto (anti fuerza bruta)
        new AspNetCoreRateLimit.RateLimitRule 
        { 
            Endpoint = "POST:/api/auth/login", 
            Period = "1m", 
            Limit = 5 
        },
        
        // ✅ Proteccion forgot password: max 3 intentos por minuto
        new AspNetCoreRateLimit.RateLimitRule 
        { 
            Endpoint = "POST:/api/auth/forgot-password", 
            Period = "1m", 
            Limit = 3 
        }
    };
});

builder.Services.AddSingleton<AspNetCoreRateLimit.IIpPolicyStore, AspNetCoreRateLimit.MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitCounterStore, AspNetCoreRateLimit.MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<AspNetCoreRateLimit.IRateLimitConfiguration, AspNetCoreRateLimit.RateLimitConfiguration>();

// 🔹 Security: Allowed Hosts protection against Host Header Injection
var allowedHosts = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?.Select(uri => 
    {
        try 
        {
            return new Uri(uri).Host;
        } 
        catch 
        { 
            return uri; 
        }
    })
    .Distinct()
    .ToArray() ?? Array.Empty<string>();

if (builder.Environment.IsDevelopment())
{
    // Allow localhost in development
    allowedHosts = allowedHosts.Concat(new [] { "localhost", "127.0.0.1" }).Distinct().ToArray();
}

// ✅ Configurar AllowedHosts correctamente para ASP.NET Core
if (!builder.Environment.IsDevelopment() && allowedHosts.Any())
{
    // Override the default "*" value
    builder.Configuration["AllowedHosts"] = string.Join(';', allowedHosts);
}

builder.WebHost.UseKestrel(options =>
{
    options.AddServerHeader = false;
});

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

// 🔹 Maintenance
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddHostedService<MaintenanceBackgroundService>();

// 🔹 JWT
var jwtKey = builder.Configuration["Jwt:Key"];

// ✅ Seguridad: NO PERMITIR arrancar la app en producción con clave por defecto
if (string.IsNullOrWhiteSpace(jwtKey) 
    || jwtKey == "your-secret-key-here-should-be-at-least-256-bits-long"
    || jwtKey.Length < 32)
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("❌ ERROR DE SEGURIDAD: JWT Key no configurada correctamente. No se puede usar la clave por defecto en producción. Debe configurar la variable de entorno Jwt__Key con una clave aleatoria de minimo 32 caracteres.");
    }
    
    // Solo en desarrollo permitimos generar clave temporal automaticamente
    jwtKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    builder.Configuration["Jwt:Key"] = jwtKey;
}

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
                Encoding.UTF8.GetBytes(jwtKey)),

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
              .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete, HttpMethods.Options, HttpMethods.Head)
              .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With", "X-Client-ID", "X-Requested-With", "Access-Control-Request-Method", "Access-Control-Request-Headers")
              .WithExposedHeaders("Content-Disposition")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(24));
    });
});

var app = builder.Build();

app.UseCors("AllowFrontend");

// ✅ Fix CORS Preflight OPTIONS requests
app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }
    
    await next();
});

// 🔹 Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prode API V1");
    });
}

app.UseIpRateLimiting();
app.UseHttpsRedirection();

// 🔹 Security Headers
if (!app.Environment.IsDevelopment())
{
    // ✅ HSTS: Forzar HTTPS permanentemente por 1 año
    app.UseHsts();

    // ✅ Middleware de cabeceras de seguridad estandar
    app.Use(async (context, next) =>
    {
        // Protege contra Clickjacking
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        
        // Evita MIME Sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // Proteccion basica XSS
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        
        // No permite embeber en iframes de dominios externos
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'";
        
        // No referrer leak
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Deshabilita cache en rutas sensibles
        if (!context.Request.Path.StartsWithSegments("/api/health") && !context.Request.Path.StartsWithSegments("/static"))
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            context.Response.Headers["Pragma"] = "no-cache";
        }

        await next();
    });
}

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
    
    await UserSeed.SeedSuperAdminAsync(userManager, roleManager);
    await ResultTypeSeed.SeedResultTypesAsync(dbContext);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
