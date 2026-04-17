namespace Prode.Application.DTOs
{
    public class MatchCreateDto
    {
        public Guid HomeTeamId { get; set; }
        public Guid AwayTeamId { get; set; }
        public DateTime MatchDate { get; set; }
        public Guid CityId { get; set; }
        public Guid CountryId { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
    }

    public class MatchUpdateDto
    {
        public Guid? HomeTeamId { get; set; }
        public Guid? AwayTeamId { get; set; }
        public DateTime? MatchDate { get; set; }
        public Guid? CityId { get; set; }
        public Guid? CountryId { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
    }

    public class MatchScoreDto
    {
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
    }
}