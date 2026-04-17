using Microsoft.AspNetCore.Http;
using Prode.Application.DTOs;
using Prode.Domain.Entities;

namespace Prode.Application.Interfaces
{
    public interface ITeamService
    {
        Task<PaginatedResponseDto<TeamDto>> GetTeamsAsync(TeamFilterDto filter);
        Task<PaginatedResponseDto<TeamDto>> GetTeamsByCountryAsync(TeamFilterDto filter);
        Task<TeamDto?> GetTeamByIdAsync(Guid id);
        Task<TeamDto> CreateTeamAsync(TeamCreateDto createDto, IFormFile? flagImage);
        Task<TeamDto> UpdateTeamAsync(Guid id, TeamUpdateDto updateDto, IFormFile? flagImage);
        Task<bool> DeleteTeamAsync(Guid id);
    }
}
