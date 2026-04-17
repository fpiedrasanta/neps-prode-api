using System;

namespace Prode.Domain.Entities
{
    public class Country
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? FlagUrl { get; set; }
        public string? IsoCode { get; set; }
        public string? IsoCode2 { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
