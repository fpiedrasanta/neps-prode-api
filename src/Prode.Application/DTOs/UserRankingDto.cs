namespace Prode.Application.DTOs
{
    public class UserRankingDto
    {
        public int Position { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int? TotalPoints { get; set; }
        public string? CountryName { get; set; }
    }

    public class UserRankingFilterDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; } // Para filtrar por nombre o email
    }

    public class UserRankingResponseDto
    {
        public List<UserRankingDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}