using Prode.Domain.Entities;
using Prode.Application.DTOs;

namespace Prode.Application.Interfaces
{
    public interface IPredictionRepository
    {
        // Métodos existentes
        Task<Prediction?> GetUserPredictionAsync(Match match, string? userId);
        Task<PredictionStatsDto> GetPredictionStatsAsync(Match match);
        
        // Nuevos métodos para CRUD
        Task<Prediction?> GetPredictionByIdAsync(Guid id);
        Task<Prediction?> GetUserPredictionByMatchIdAsync(Guid matchId, string userId);
        Task<List<Prediction>> GetPredictionsWithoutResultTypeAsync(string userId);
        Task<List<Prediction>> GetPredictionsWithoutResultTypeForFinishedMatchesAsync();
        Task<List<Prediction>> GetPredictionsWithResultTypeWithoutPostAsync();
        Task<List<ResultType>> GetAllResultTypesAsync();
        Task UpdateUserTotalPointsAsync(string userId, int pointsToAdd);
        Task<Prediction> CreatePredictionAsync(Prediction prediction);
        Task<Prediction> UpdatePredictionAsync(Prediction prediction);
        Task<bool> DeletePredictionAsync(Guid id);

        // Obtener IDs de usuarios que NO hicieron prediccion para un partido
        Task<IEnumerable<string>> GetUserIdsWithoutPredictionForMatchAsync(Guid matchId);
    }
}
