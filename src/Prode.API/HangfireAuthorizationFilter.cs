using Hangfire.Dashboard;
using System.Security.Claims;

namespace Prode.API;

/// <summary>
/// Filtro de autorización para el Dashboard de Hangfire.
/// Requiere que el usuario esté autenticado y tenga rol Admin.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // En desarrollo, permitir acceso sin autenticación si está configurado
        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            var requireAuth = config.GetSection("Hangfire:Dashboard").GetValue<bool>("RequireAuthentication");
            if (!requireAuth)
                return true;
        }

        // Usuario debe estar autenticado
        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        // Usuario debe tener rol Admin
        if (!httpContext.User.IsInRole("Admin"))
            return false;

        return true;
    }
}