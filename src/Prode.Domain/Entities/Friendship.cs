namespace Prode.Domain.Entities
{
    public class Friendship
    {
        public Guid Id { get; set; }
        
        // Usuario que envía la solicitud
        public string RequesterId { get; set; }
        public virtual ApplicationUser? Requester { get; set; }
        
        // Usuario que recibe la solicitud
        public string AddresseeId { get; set; }
        public virtual ApplicationUser? Addressee { get; set; }
        
        // Estado de la solicitud
        public FriendshipStatus Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public enum FriendshipStatus
    {
        Pending,    // Solicitud enviada, esperando respuesta
        Accepted,   // Amigos confirmados
        Declined,   // Solicitud rechazada
        Blocked     // Usuario bloqueado (no recibe más solicitudes)
    }
}