namespace Prode.Application.DTOs
{
    public class PostDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        
        public Guid MatchId { get; set; }
        public string HomeTeamName { get; set; } = string.Empty;
        public string? HomeTeamFlagUrl { get; set; }
        public string AwayTeamName { get; set; } = string.Empty;
        public string? AwayTeamFlagUrl { get; set; }
        
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        
        public int? HomePrediction { get; set; }
        public int? AwayPrediction { get; set; }
        
        public int? PointsEarned { get; set; }
        public DateTime MatchDate { get; set; }
        
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        public List<CommentDto> Comments { get; set; } = new();
    }

    public class CommentDto
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string? UserAvatarUrl { get; set; }
        
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCommentDto
    {
        public string Content { get; set; } = string.Empty;
    }
}