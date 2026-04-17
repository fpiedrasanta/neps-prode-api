namespace Prode.Application.DTOs
{
    public class TeamDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? FlagUrl { get; set; }
        public Guid CountryId { get; set; }
        public string? CountryName { get; set; }
    }

    public class TeamCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid CountryId { get; set; }
    }

    public class TeamUpdateDto
    {
        public string? Name { get; set; }
        public string? FlagUrl { get; set; }
        public Guid? CountryId { get; set; }
    }

    public class TeamFilterDto
    {
        public Guid? CountryId { get; set; }
        public string? Search { get; set; }
        public string? OrderBy { get; set; } = "Name";
        public bool OrderDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}