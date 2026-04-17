using System;

namespace Prode.Domain.Entities
{
    public class City
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CountryId { get; set; }
        public virtual Country Country { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        public bool CountryIsNull()
        {
            return this.Country == null;
        }

        public string? GetCountryFlagUrl()
        {
            return this.Country != null ? this.Country.FlagUrl : null;
        }

        public Guid? GetCountryId()
        {
            return this.Country != null ? this.Country.Id : null;
        }

        public string? GetCountryIsoCode()
        {
            return this.Country != null ? this.Country.IsoCode : null;
        }

        public string? GetCountryIsoCode2()
        {
            return this.Country != null ? this.Country.IsoCode2 : null;
        }

        public string? GetCountryName()
        {
            return this.Country != null ? this.Country.Name : null;
        }
    }
}
