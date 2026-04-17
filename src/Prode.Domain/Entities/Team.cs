using System;

namespace Prode.Domain.Entities
{
    public class Team
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? FlagUrl { get; set; }
        public Guid CountryId { get; set; }
        public virtual Country Country { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        public string? GetCountryFlagUrl()
        {
            return this.Country != null ? this.Country.FlagUrl : "";
        }

        public Guid? GetCountryId()
        {
            return this.Country != null ? this.Country.Id : null;
        }

        public string? GetCountryIsoCode()
        {
            return this.Country != null ? this.Country.IsoCode : "";
        }

        public string? GetCountryIsoCode2()
        {
            return this.Country != null ? this.Country.IsoCode2 : "";
        }

        public string? GetCountryName()
        {
            return this.Country != null ? this.Country.Name : "";
        }
    }
}
