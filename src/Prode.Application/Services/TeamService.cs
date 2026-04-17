using Microsoft.AspNetCore.Http;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using System.Linq.Expressions;

namespace Prode.Application.Services
{
    public class TeamService : ITeamService
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IFileService _fileService;

        public TeamService(ITeamRepository teamRepository, IFileService fileService)
        {
            _teamRepository = teamRepository;
            _fileService = fileService;
        }

        public async Task<PaginatedResponseDto<TeamDto>> GetTeamsAsync(TeamFilterDto filter)
        {
            // Construir la expresión de búsqueda
            Expression<Func<Team, bool>> searchExpression = null;
            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                searchExpression = t => t.Name.ToLower().Contains(searchLower);
            }

            // Construir la expresión de ordenamiento
            Expression<Func<Team, object>> orderByExpression = filter.OrderBy?.ToLower() switch
            {
                "name" => t => t.Name,
                _ => t => t.Name // Default order by Name
            };

            // Obtener los equipos con paginación y filtrado
            var teams = await _teamRepository.GetTeamsAsync(
                filter.PageNumber,
                filter.PageSize,
                searchExpression,
                orderByExpression,
                filter.OrderDescending
            );

            // Mapear a DTOs
            var teamDtos = teams.Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                FlagUrl = t.FlagUrl,
                CountryId = t.CountryId,
                CountryName = t.Country?.Name
            }).ToList();

            // Calcular el total de registros para la paginación
            var totalCount = await _teamRepository.GetTeamsCountAsync(searchExpression);

            return new PaginatedResponseDto<TeamDto>
            {
                Items = teamDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        public async Task<PaginatedResponseDto<TeamDto>> GetTeamsByCountryAsync(TeamFilterDto filter)
        {
            if (!filter.CountryId.HasValue)
            {
                throw new ArgumentException("El CountryId es obligatorio para este endpoint.");
            }

            // Construir la expresión de búsqueda
            Expression<Func<Team, bool>> searchExpression = null;
            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                searchExpression = t => t.Name.ToLower().Contains(searchLower);
            }

            // Construir la expresión de ordenamiento
            Expression<Func<Team, object>> orderByExpression = filter.OrderBy?.ToLower() switch
            {
                "name" => t => t.Name,
                _ => t => t.Name // Default order by Name
            };

            // Obtener los equipos con paginación y filtrado
            var teams = await _teamRepository.GetTeamsByCountryAsync(
                filter.CountryId.Value,
                filter.PageNumber,
                filter.PageSize,
                searchExpression,
                orderByExpression,
                filter.OrderDescending
            );

            // Mapear a DTOs
            var teamDtos = teams.Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                FlagUrl = t.FlagUrl,
                CountryId = t.CountryId,
                CountryName = t.Country?.Name
            }).ToList();

            // Calcular el total de registros para la paginación
            var totalCount = await _teamRepository.GetTeamsCountByCountryAsync(filter.CountryId.Value, searchExpression);

            return new PaginatedResponseDto<TeamDto>
            {
                Items = teamDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        public async Task<TeamDto?> GetTeamByIdAsync(Guid id)
        {
            var team = await _teamRepository.GetTeamByIdAsync(id);
            if (team == null)
            {
                return null;
            }

            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                FlagUrl = team.FlagUrl,
                CountryId = team.CountryId,
                CountryName = team.Country?.Name
            };
        }

        public async Task<TeamDto> CreateTeamAsync(TeamCreateDto createDto, IFormFile? flagImage)
        {
            var flagUrl = flagImage != null ? await _fileService.SaveFlagAsync(flagImage.OpenReadStream(), flagImage.FileName) : null;

            var team = new Team
            {
                Name = createDto.Name,
                FlagUrl = flagUrl,
                CountryId = createDto.CountryId,
                IsActive = true
            };

            var createdTeam = await _teamRepository.CreateTeamAsync(team);

            return new TeamDto
            {
                Id = createdTeam.Id,
                Name = createdTeam.Name,
                FlagUrl = createdTeam.FlagUrl,
                CountryId = createdTeam.CountryId,
                CountryName = createdTeam.Country?.Name
            };
        }

        public async Task<TeamDto> UpdateTeamAsync(Guid id, TeamUpdateDto updateDto, IFormFile? flagImage)
        {
            var existingTeam = await _teamRepository.GetTeamByIdAsync(id);
            if (existingTeam == null)
            {
                throw new Exception("Equipo no encontrado.");
            }

            if (!string.IsNullOrEmpty(updateDto.Name))
                existingTeam.Name = updateDto.Name;
            if (flagImage != null)
            {
                // Eliminar bandera anterior si existe
                if (!string.IsNullOrEmpty(existingTeam.FlagUrl))
                {
                    // No hay método DeleteFlagAsync, pero podemos sobrescribir el archivo
                }
                existingTeam.FlagUrl = await _fileService.SaveFlagAsync(flagImage.OpenReadStream(), flagImage.FileName);
            }
            if (updateDto.CountryId.HasValue)
                existingTeam.CountryId = updateDto.CountryId.Value;

            var updatedTeam = await _teamRepository.UpdateTeamAsync(existingTeam);

            return new TeamDto
            {
                Id = updatedTeam.Id,
                Name = updatedTeam.Name,
                FlagUrl = updatedTeam.FlagUrl,
                CountryId = updatedTeam.CountryId,
                CountryName = updatedTeam.Country?.Name
            };
        }

        public async Task<bool> DeleteTeamAsync(Guid id)
        {
            return await _teamRepository.DeleteTeamAsync(id);
        }
    }
}