namespace Prode.Domain.Entities
{
    public class Comment
    {
        public Guid Id { get; set; }
        
        // Usuario que comentó
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser? User { get; set; }
        
        // Post al que pertenece el comentario
        public Guid PostId { get; set; }
        public virtual Post? Post { get; set; }
        
        // Contenido del comentario
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}