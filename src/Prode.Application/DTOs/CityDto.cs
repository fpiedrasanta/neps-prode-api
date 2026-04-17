namespace Prode.Application.DTOs
{
    public class CityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CountryId { get; set; }
        public string? CountryName { get; set; }
    }

    public class CityCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public Guid CountryId { get; set; }
    }

    public class CityUpdateDto
    {
        public string? Name { get; set; }
        public Guid? CountryId { get; set; }
    }

    public class CityFilterDto
    {
        public Guid CountryId { get; set; }
        public string? Search { get; set; }
        public string? OrderBy { get; set; } = "Name";
        public bool OrderDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}