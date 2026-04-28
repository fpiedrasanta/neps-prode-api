namespace Prode.Application.Interfaces
{
    public interface IMaintenanceService
    {
        // Calcular puntos de predicciones sin ResultType de partidos finalizados
        Task<int> CalculatePointsForFinishedMatchesAsync();
        
        // Crear posts para predicciones que ya tienen ResultType pero no tienen post
        Task<int> CreatePostsForPredictionsAsync();
        
        // Eliminar solicitudes de amistad expiradas
        Task<int> DeleteExpiredFriendRequestsAsync(int expirationDays);

        // Enviar recordatorios de partidos que estan por empezar
        Task ProcessMatchRemindersAsync();

        // Enviar notificaciones de partidos que empezaron
        Task ProcessMatchStartedNotificationsAsync();

        // Enviar notificaciones de partidos finalizados y puntos calculados
        Task ProcessMatchFinishedNotificationsAsync();
    }
}
