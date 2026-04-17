using Prode.Application.DTOs;
using Prode.Application.Helpers;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Prode.Domain.Enums;

namespace Prode.Application.Services
{
    public class MatchService : IMatchService
    {
        private readonly IMatchRepository _matchRepository;
        private readonly IPredictionRepository _predictionRepository;

        public MatchService(
            IMatchRepository matchRepository,
            IPredictionRepository predictionRepository
            )
        {
            _matchRepository = matchRepository;
            _predictionRepository = predictionRepository;
        }

        public async Task<PaginatedResponseDto<MatchResponseDto>> GetMatchesByStatusAsync(
            MatchFilterDto filter)
        {
            // Obtener el total de registros para la paginación usando COUNT(*)
            var totalCount = await _matchRepository.GetMatchesCountByStatusAsync(filter);
            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);
            
            // Obtener solo los registros paginados
            var matches = await _matchRepository.GetMatchesByStatusAsync(filter);

            var result = new PaginatedResponseDto<MatchResponseDto>
            {
                Items = new List<MatchResponseDto>(),
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = totalPages
            };

            foreach (var match in matches)
            {
                Prediction? prediction = await this._predictionRepository.GetUserPredictionAsync(match, filter.UserId);
                MatchResponseDto matchResponseDto = await MapToResponseDtoAsync(match, prediction, filter.Status);
                matchResponseDto.PredictionStats = await this._predictionRepository.GetPredictionStatsAsync(match);
                result.Items.Add(matchResponseDto);
            }

            return result;
        }

        public async Task<MatchResponseDto?> GetMatchByIdAsync(Guid id)
        {
            var match = await _matchRepository.GetMatchByIdAsync(id);
            if (match == null)
            {
                return null;
            }

            return await MapToResponseDtoAsync(match, null, null);
        }

        public async Task<MatchResponseDto> CreateMatchAsync(MatchCreateDto createDto)
        {
            var match = new Match
            {
                HomeTeamId = createDto.HomeTeamId,
                AwayTeamId = createDto.AwayTeamId,
                MatchDate = DateTimeHelper.NormalizeForPersistence(createDto.MatchDate, "Crear Partido"),
                CityId = createDto.CityId,
                CountryId = createDto.CountryId,
                HomeScore = createDto.HomeScore,
                AwayScore = createDto.AwayScore,
                IsActive = true
            };

            var createdMatch = await _matchRepository.CreateMatchAsync(match);
            return await MapToResponseDtoAsync(createdMatch, null, null);
        }

        public async Task<MatchResponseDto> UpdateMatchAsync(Guid id, MatchUpdateDto updateDto)
        {
            var existingMatch = await _matchRepository.GetMatchByIdAsync(id);
            if (existingMatch == null)
            {
                throw new Exception("Partido no encontrado.");
            }

            if (updateDto.HomeTeamId.HasValue)
                existingMatch.HomeTeamId = updateDto.HomeTeamId.Value;
            if (updateDto.AwayTeamId.HasValue)
                existingMatch.AwayTeamId = updateDto.AwayTeamId.Value;
            if (updateDto.MatchDate.HasValue)
                existingMatch.MatchDate = DateTimeHelper.NormalizeForPersistence(updateDto.MatchDate.Value, "Actualizar Partido");
            if (updateDto.CityId.HasValue)
                existingMatch.CityId = updateDto.CityId.Value;
            if (updateDto.CountryId.HasValue)
                existingMatch.CountryId = updateDto.CountryId.Value;
            if (updateDto.HomeScore.HasValue)
                existingMatch.HomeScore = updateDto.HomeScore;
            if (updateDto.AwayScore.HasValue)
                existingMatch.AwayScore = updateDto.AwayScore;

            var updatedMatch = await _matchRepository.UpdateMatchAsync(existingMatch);
            return await MapToResponseDtoAsync(updatedMatch, null, null);
        }

        public async Task<MatchResponseDto> UpdateMatchScoresAsync(Guid id, MatchScoreDto scoreDto)
        {
            var updatedMatch = await _matchRepository.UpdateMatchScoresAsync(id, scoreDto.HomeScore, scoreDto.AwayScore);
            return await MapToResponseDtoAsync(updatedMatch, null, null);
        }

        public async Task<bool> DeleteMatchAsync(Guid id)
        {
            return await _matchRepository.DeleteMatchAsync(id);
        }

        private async Task<MatchResponseDto> MapToResponseDtoAsync(
            Match match, 
            Prediction? prediction, 
            MatchStatusFilter? status)
        {
            MatchResponseDto matchResponseDto = new MatchResponseDto
            {
                Status = status,
                AwayScore = match.AwayScore,
                AwayTeam = match.AwayTeam == null ? null : new TeamInfoDto
                {
                    Country = match.GetAwayTeamCountryId() == null ? null : new CountryDto
                    {
                        FlagUrl = match.GetAwayTeamCountryFlagUrl(),
                        Id = match.GetAwayTeamCountryId(),
                        IsoCode = match.GetAwayTeamCountryIsoCode(),
                        IsoCode2 = match.GetAwayTeamCountryIsoCode2(),
                        Name = match.GetAwayTeamCountryName()
                    },
                    FlagUrl = match.GetAwayTeamFlagUrl(),
                    Id = match.GetAwayTeamId(),
                    Name = match.GetAwayTeamName()
                },
                City = match.CityIsNull() ? null : new CityInfoDto
                {
                    Country = match.CityCountryIsNull() ? null : new CountryDto
                    {
                        FlagUrl = match.GetCityCountryFlagUrl(),
                        Id = match.GetCityCountryId(),
                        IsoCode = match.GetCityCountryIsoCode(),
                        IsoCode2 = match.GetCityCountryIsoCode2(),
                        Name = match.GetCityCountryName()
                    },
                    Id = match.GetCityId(),
                    Name = match.GetCityName()
                },
                Country = new CountryDto
                {
                    FlagUrl = match.GetCountryFlagUrl(),
                    Id = match.GetCountryId(),
                    IsoCode = match.GetCountryIsoCode(),
                    IsoCode2 = match.GetCountryIsoCode2(),
                    Name = match.GetCountryName()
                },
                HomeScore = match.HomeScore,
                HomeTeam = new TeamInfoDto
                {
                    Country = match.GetHomeTeamCountryId() == null ? null : new CountryDto
                    {
                        FlagUrl = match.GetHomeTeamCountryFlagUrl(),
                        Id = match.GetHomeTeamCountryId(),
                        IsoCode = match.GetHomeTeamCountryIsoCode(),
                        IsoCode2 = match.GetHomeTeamCountryIsoCode2(),
                        Name = match.GetHomeTeamCountryName()
                    },
                    FlagUrl = match.GetHomeTeamFlagUrl(),
                    Id = match.GetHomeTeamId(),
                    Name = match.GetHomeTeamName()
                },
                Id = match.Id,
                MatchDate = match.MatchDate,
                UserPrediction = prediction == null ? null : new UserPredictionDto
                {
                    Id = prediction.Id,
                    AwayGoals = prediction.AwayGoals,
                    CreatedAt = prediction.CreatedAt,
                    HomeGoals = prediction.HomeGoals,
                    Points = prediction.GetResultTypePoints(),
                    UpdatedAt = prediction.UpdatedAt
                }
            };
            
            return matchResponseDto;
        }
    }
}