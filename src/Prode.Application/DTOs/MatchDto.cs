using Prode.Domain.Enums;

namespace Prode.Application.DTOs
{
    public class MatchDto
    {
        public Guid Id { get; set; }
        public Guid HomeTeamId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string? HomeTeamFlagUrl { get; set; }
        public Guid AwayTeamId { get; set; }
        public string AwayTeamName { get; set; } = string.Empty;
        public string? AwayTeamFlagUrl { get; set; }
        public DateTime MatchDate { get; set; }
        public Guid CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public Guid CountryId { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public string? CountryFlagUrl { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        
        // Predicción del usuario actual
        public UserPredictionDto? UserPrediction { get; set; }
    }
}
