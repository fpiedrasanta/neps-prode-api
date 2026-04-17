namespace Prode.Application.DTOs
{
    public class CountryDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? IsoCode { get; set; }
        public string? IsoCode2 { get; set; }
        public string? FlagUrl { get; set; }
    }

    public class CountryFilterDto
    {
        public string? Search { get; set; }
        public string? OrderBy { get; set; } = "Name";
        public bool OrderDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
