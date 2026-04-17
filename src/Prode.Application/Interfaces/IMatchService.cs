using Prode.Application.DTOs;
using Prode.Domain.Enums;

namespace Prode.Application.Interfaces
{
    public interface IMatchService
    {
        Task<PaginatedResponseDto<MatchResponseDto>> GetMatchesByStatusAsync(MatchFilterDto filter);
        Task<MatchResponseDto?> GetMatchByIdAsync(Guid id);
        Task<MatchResponseDto> CreateMatchAsync(MatchCreateDto createDto);
        Task<MatchResponseDto> UpdateMatchAsync(Guid id, MatchUpdateDto updateDto);
        Task<MatchResponseDto> UpdateMatchScoresAsync(Guid id, MatchScoreDto scoreDto);
        Task<bool> DeleteMatchAsync(Guid id);
    }
}
