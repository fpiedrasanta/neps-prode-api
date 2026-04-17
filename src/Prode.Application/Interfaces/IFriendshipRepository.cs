using Prode.Domain.Entities;

namespace Prode.Application.Interfaces
{
    public interface IFriendshipRepository
    {
        // Obtener resumen de amistades de un usuario
        Task<(List<Friendship> Friends, List<Friendship> SentRequests, List<Friendship> ReceivedRequests, ApplicationUser me)> GetFriendshipSummaryAsync(string userId);
        
        // Buscar usuarios que no son amigos
        Task<List<ApplicationUser>> SearchUsersAsync(string userId, string search);
        
        // Obtener amistad entre dos usuarios
        Task<Friendship?> GetFriendshipAsync(string requesterId, string addresseeId);
        
        // Obtener amistad por ID
        Task<Friendship?> GetFriendshipByIdAsync(Guid id);
        
        // Crear solicitud de amistad
        Task<Friendship> CreateFriendRequestAsync(string requesterId, string addresseeId);
        
        // Aceptar solicitud de amistad
        Task AcceptFriendRequestAsync(Guid friendshipId);
        
        // Rechazar/bloquear solicitud
        Task DeclineFriendRequestAsync(Guid friendshipId);
        
        // Eliminar amistad
        Task RemoveFriendAsync(Guid friendshipId);
        
        // Obtener solicitudes recibidas
        Task<List<Friendship>> GetReceivedRequestsAsync(string userId);
        
        // Eliminar solicitudes pendientes expiradas
        Task<int> DeleteExpiredPendingRequestsAsync(DateTime expirationDate);
    }
}
