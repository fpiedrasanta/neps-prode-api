using Prode.Application.DTOs;

namespace Prode.Application.Interfaces
{
    public interface IPredictionService
    {
        Task<PredictionDto> CreatePredictionAsync(string userId, PredictionCreateDto createDto);
        Task<PredictionDto> UpdatePredictionAsync(string userId, Guid id, PredictionUpdateDto updateDto);
        Task<bool> DeletePredictionAsync(string userId, Guid id);
        Task<PredictionDto?> GetPredictionByIdAsync(string userId, Guid id);
        Task<List<PredictionDto>> GetPredictionsWithoutResultTypeAsync(string userId);
        Task<int> AssignResultTypesToPredictionsAsync(string userId);
    }
}