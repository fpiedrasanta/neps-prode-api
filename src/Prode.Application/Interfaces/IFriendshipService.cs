using Prode.Application.DTOs;

namespace Prode.Application.Interfaces
{
    public interface IFriendshipService
    {
        // Obtener resumen de amistades
        Task<FriendshipSummaryDto> GetFriendshipSummaryAsync(string userId);
        
        // Buscar usuarios para agregar como amigos
        Task<List<UserSearchDto>> SearchUsersAsync(string userId, string search);
        
        // Enviar solicitud de amistad
        Task<FriendshipDto> SendFriendRequestAsync(string currentUserId, string targetUserId);
        
        // Aceptar solicitud de amistad
        Task AcceptFriendRequestAsync(string currentUserId, Guid friendshipId);
        
        // Rechazar solicitud de amistad
        Task DeclineFriendRequestAsync(string currentUserId, Guid friendshipId);
        
        // Eliminar amigo
        Task RemoveFriendAsync(Guid friendshipId);
        
    }
}
