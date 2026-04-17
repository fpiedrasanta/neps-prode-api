using System;

namespace Prode.Application.DTOs
{
    public class MatchFilterDto
    {
        public int PageNumber { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
        
        public MatchStatusFilter Status { get; set; } = MatchStatusFilter.Upcoming;
        
        public string? UserId { get; set; }
        
        // Configuración para el tiempo límite de edición (en minutos antes del partido)
        public int MinutesBeforeMatchToLock { get; set; } = 15;
        
        // Búsqueda por nombre de país
        public string? TeamNameSearch { get; set; }
    }

    public enum MatchStatusFilter
    {
        Upcoming = 1,
        InProgress = 2,
        Finished = 3
    }
}