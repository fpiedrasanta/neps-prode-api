using System;

namespace Prode.Domain.Entities
{
    public class Match
    {
        public Guid Id { get; set; }
        
        public Guid HomeTeamId { get; set; }
        public virtual Team HomeTeam { get; set; } = null!;
        
        public Guid AwayTeamId { get; set; }
        public virtual Team AwayTeam { get; set; } = null!;
        
        public DateTime MatchDate { get; set; }
        
        public Guid CityId { get; set; }
        public virtual City City { get; set; } = null!;
        
        public Guid CountryId { get; set; }
        public virtual Country Country { get; set; } = null!;
        
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Relaciones
        public virtual ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();

        public bool CityCountryIsNull()
        {
            return this.City != null ? this.City.CountryIsNull() : true;
        }

        public bool CityIsNull()
        {
            return this.City == null;
        }

        public bool CountryIsNull()
        {
            return this.Country == null;
        }

        public string? GetAwayTeamCountryFlagUrl()
        {
            return this.AwayTeam != null ? this.AwayTeam.GetCountryFlagUrl() : null;
        }

        public Guid? GetAwayTeamCountryId()
        {
            return this.AwayTeam != null ? this.AwayTeam.GetCountryId() : null;
        }

        public string? GetAwayTeamCountryIsoCode()
        {
            return this.AwayTeam != null ? this.AwayTeam.GetCountryIsoCode() : null;
        }

        public string? GetAwayTeamCountryIsoCode2()
        {
            return this.AwayTeam != null ? this.AwayTeam.GetCountryIsoCode2() : null;
        }

        public string? GetAwayTeamCountryName()
        {
            return this.AwayTeam != null ? this.AwayTeam.GetCountryName() : null;
        }

        public string? GetAwayTeamFlagUrl()
        {
            return this.AwayTeam != null ? this.AwayTeam.FlagUrl : null;
        }

        public Guid? GetAwayTeamId()
        {
            return this.AwayTeam != null ? this.AwayTeam.Id : null;
        }

        public string? GetAwayTeamName()
        {
            return this.AwayTeam != null ? this.AwayTeam.Name : null;
        }

        public string? GetCityCountryFlagUrl()
        {
            return this.City != null ? this.City.GetCountryFlagUrl() : null;
        }

        public Guid? GetCityCountryId()
        {
            return this.City != null ? this.City.GetCountryId() : null;
        }

        public string? GetCityCountryIsoCode()
        {
            return this.City != null ? this.City.GetCountryIsoCode() : null;
        }

        public string? GetCityCountryIsoCode2()
        {
            return this.City != null ? this.City.GetCountryIsoCode2() : null;
        }

        public string? GetCityCountryName()
        {
            return this.City != null ? this.City.GetCountryName() : null;
        }

        public Guid? GetCityId()
        {
            return this.City != null ? this.City.Id : null;
        }

        public string? GetCityName()
        {
            return this.City != null ? this.City.Name : null;
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

        public string? GetHomeTeamCountryFlagUrl()
        {
            return this.HomeTeam != null ? this.HomeTeam.GetCountryFlagUrl() : null;
        }

        public Guid? GetHomeTeamCountryId()
        {
            return this.HomeTeam != null ? this.HomeTeam.GetCountryId() : null;
        }

        public string? GetHomeTeamCountryIsoCode()
        {
            return this.HomeTeam != null ? this.HomeTeam.GetCountryIsoCode() : null;
        }

        public string? GetHomeTeamCountryIsoCode2()
        {
            return this.HomeTeam != null ? this.HomeTeam.GetCountryIsoCode2() : null;
        }

        public string? GetHomeTeamCountryName()
        {
            return this.HomeTeam != null ? this.HomeTeam.GetCountryName() : null;
        }

        public string? GetHomeTeamFlagUrl()
        {
            return this.HomeTeam != null ? this.HomeTeam.FlagUrl : null;
        }

        public Guid? GetHomeTeamId()
        {
            return this.HomeTeam != null ? this.HomeTeam.Id : null;
        }

        public string? GetHomeTeamName()
        {
            return this.HomeTeam != null ? this.HomeTeam.Name : null;
        }

        // Flags para notificaciones (evita envios repetidos)
        public bool ReminderNotificationSent { get; set; } = false;
        public bool StartedNotificationSent { get; set; } = false;
        public bool FinishedNotificationSent { get; set; } = false;
    }
}
