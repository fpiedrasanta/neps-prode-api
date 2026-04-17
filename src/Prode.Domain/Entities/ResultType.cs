using System;

namespace Prode.Domain.Entities
{
    public class ResultType
    {
        public Guid Id { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public int Points { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Relaciones
        public virtual ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }
}