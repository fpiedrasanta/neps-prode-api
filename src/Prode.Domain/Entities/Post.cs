namespace Prode.Domain.Entities
{
    public class Post
    {
        public Guid Id { get; set; }
        
        // Usuario que creó el post
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        
        // Partido relacionado
        public Guid? MatchId { get; set; }
        public virtual Match? Match { get; set; }

        // Post Especial (Dashboard)
        public bool IsSpecialPost { get; set; } = false;
        
        // Título del post (solo para posts especiales, admite HTML)
        public string? Title { get; set; }
        
        // Predicción relacionada
        public Guid? PredictionId { get; set; }
        public virtual Prediction? Prediction { get; set; }
        
        // Contenido del post
        public string Content { get; set; } = string.Empty;
        
        // Puntos obtenidos (solo para posts de predicciones)
        public int? PointsEarned { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Comentarios del post
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}