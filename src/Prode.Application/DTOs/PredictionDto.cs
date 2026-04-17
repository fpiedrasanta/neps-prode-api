namespace Prode.Application.DTOs
{
    public class PredictionDto
    {
        public Guid Id { get; set; }
        public Guid MatchId { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? Points { get; set; }
        public string? ResultTypeName { get; set; }
    }

    public class PredictionCreateDto
    {
        public Guid MatchId { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
    }

    public class PredictionUpdateDto
    {
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
    }
}