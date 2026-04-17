using System;
using Prode.Domain.Enums;

namespace Prode.Domain.Entities
{
    public class Prediction
    {
        public Guid Id { get; set; }
        
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        
        public int HomeGoals { get; set; }
        
        public int AwayGoals { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Relaciones
        public Guid MatchId { get; set; }
        public virtual Match Match { get; set; } = null!;
        
        // Resultado del pronóstico
        public virtual ResultType? ResultType { get; set; }

        public int? GetResultTypePoints()
        {
            return this.ResultType != null ? this.ResultType.Points : null;
        }
    }
}
