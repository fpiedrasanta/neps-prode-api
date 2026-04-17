using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Prode.Application.Interfaces;

namespace Prode.Infrastructure.Services
{
    public class MaintenanceBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MaintenanceBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _intervalInMinutes;
        private readonly int _friendRequestExpirationDays;

        public MaintenanceBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<MaintenanceBackgroundService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            
            _intervalInMinutes = configuration.GetValue<int>("BackgroundService:IntervalInMinutes", 60);
            _friendRequestExpirationDays = configuration.GetValue<int>("BackgroundService:FriendRequestExpirationDays", 7);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Maintenance Background Service iniciado. Intervalo: {Interval} minutos", _intervalInMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Iniciando tarea de mantenimiento a {Time}", DateTime.UtcNow);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var maintenanceService = scope.ServiceProvider.GetRequiredService<IMaintenanceService>();

                        // 1. Calcular puntos de partidos finalizados (asigna ResultType a predicciones sin puntos)
                        var pointsUpdated = await maintenanceService.CalculatePointsForFinishedMatchesAsync();
                        _logger.LogInformation("Puntos calculados para {Count} predicciones", pointsUpdated);

                        // 2. Crear posts para predicciones que ya tienen ResultType pero no tienen post
                        var postsCreated = await maintenanceService.CreatePostsForPredictionsAsync();
                        _logger.LogInformation("Posts creados: {Count}", postsCreated);

                        // 3. Eliminar solicitudes de amistad expiradas
                        var deletedRequests = await maintenanceService.DeleteExpiredFriendRequestsAsync(_friendRequestExpirationDays);
                        _logger.LogInformation("Solicitudes de amistad expiradas eliminadas: {Count}", deletedRequests);
                    }

                    _logger.LogInformation("Tarea de mantenimiento completada a {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al ejecutar tarea de mantenimiento a {Time}", DateTime.UtcNow);
                }

                // Esperar el intervalo configurado
                await Task.Delay(TimeSpan.FromMinutes(_intervalInMinutes), stoppingToken);
            }

            _logger.LogInformation("Maintenance Background Service detenido a {Time}", DateTime.UtcNow);
        }
    }
}