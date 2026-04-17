namespace Prode.Application.DTOs
{
    public class CountryCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? FlagUrl { get; set; }
        public string? IsoCode { get; set; }
        public string? IsoCode2 { get; set; }
    }
}
