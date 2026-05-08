using Hangfire.Dashboard;
using System.Security.Claims;

namespace Prode.API;

/// <summary>
/// Filtro de autorización para el Dashboard de Hangfire.
/// 
/// En producción:
/// - Requiere JWT válido + rol Admin (vía header Authorization: Bearer)
/// - Si el JWT no está presente, redirige a una URL de login/configurable
///   para que puedas pegar el token manualmente, o usa una cookie de sesión.
///
/// En desarrollo local sin RequireAuthentication se permite todo.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var requireAuth = config.GetSection("Hangfire:Dashboard").GetValue<bool>("RequireAuthentication");

        // Modo desarrollo: si no requiere auth, permitir todo
        if (env.IsDevelopment() && !requireAuth)
            return true;

        // 1️. Verificar si el usuario ya está autenticado vía JWT (API calls desde el frontend)
        if (httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("Admin"))
            return true;

        // 2️. Verificar si tiene una cookie "hangfire_token" con un JWT válido
        var cookieToken = httpContext.Request.Cookies["hangfire_token"];
        if (!string.IsNullOrEmpty(cookieToken) && ValidateToken(cookieToken, config))
            return true;

        // 3️. Query string ?token=xxx (útil para acceso rápido desde browser)
        var queryToken = httpContext.Request.Query["token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(queryToken) && ValidateToken(queryToken, config))
            return true;

        // No autorizado - devolver 401
        return false;
    }

    private static bool ValidateToken(string token, IConfiguration config)
    {
        try
        {
            var jwtKey = config["Jwt:Key"] ?? "";
            var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var key = System.Text.Encoding.UTF8.GetBytes(jwtKey);
            var principal = tokenHandler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = config["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                NameClaimType = "sub",
                RoleClaimType = ClaimTypes.Role
            }, out _);

            return principal.IsInRole("Admin");
        }
        catch
        {
            return false;
        }
    }
}
