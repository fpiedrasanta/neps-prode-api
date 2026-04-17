using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prode.Domain.Entities;
using Prode.Domain.Enums;

namespace Prode.Application.DTOs
{
    public class MatchResponseDto
    {
        public Guid? Id { get; set; }
        public TeamInfoDto? HomeTeam { get; set; } = null!;
        public TeamInfoDto? AwayTeam { get; set; } = null!;
        public DateTime? MatchDate { get; set; }
        public CityInfoDto? City { get; set; } = null!;
        public CountryDto? Country { get; set; } = null!;
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        
        // Información del pronóstico del usuario
        public UserPredictionDto? UserPrediction { get; set; }
        
        // Puntos obtenidos (solo para partidos finalizados)
        public int? Points { get; set; }
        
        // Estadísticas de predicciones
        public PredictionStatsDto? PredictionStats { get; set; }
        public MatchStatusFilter? Status { get; set; }
    }

    public class TeamInfoDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? FlagUrl { get; set; }
        public CountryDto? Country { get; set; } = null!;
    }

    public class CityInfoDto
    {
        public Guid? Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public CountryDto? Country { get; set; } = null!;
    }

    public class UserPredictionDto
    {
        public Guid? Id { get; set; }
        public int HomeGoals { get; set; }
        public int AwayGoals { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? Points { get; set; }
    }
}