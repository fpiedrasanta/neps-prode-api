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
    }
}
