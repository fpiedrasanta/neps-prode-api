using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;

namespace Prode.Application.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private const int MaxFriends = 5;

        public FriendshipService(IFriendshipRepository friendshipRepository)
        {
            _friendshipRepository = friendshipRepository;
        }

        public async Task<FriendshipSummaryDto> GetFriendshipSummaryAsync(string userId)
        {
            var (friends, sentRequests, receivedRequests, me) = await _friendshipRepository.GetFriendshipSummaryAsync(userId);

            return new FriendshipSummaryDto
            {
                Friends = friends.Select(f => MapToDto(f, userId)).ToList(),
                SentRequests = sentRequests.Select(f => MapToDto(f, userId)).ToList(),
                ReceivedRequests = receivedRequests.Select(f => MapToDto(f, userId)).ToList(),
                CurrentUser = new
                {
                    id = me.Id,
                    email = me.Email,
                    fullName = me.FullName,
                    avatarUrl = me.AvatarPath,
                    totalPoints = me.TotalPoints,
                    countryName = me.Country != null ? me.Country.Name : ""
                }
            };
        }

        public async Task<List<UserSearchDto>> SearchUsersAsync(string userId, string search)
        {
            var users = await _friendshipRepository.SearchUsersAsync(userId.ToString(), search);

            // Obtener solicitudes recibidas para marcar si el usuario ya envió solicitud
            var receivedRequests = await _friendshipRepository.GetReceivedRequestsAsync(userId.ToString());
            var receivedRequestUserIds = receivedRequests.Select(r => r.RequesterId).ToHashSet();

            return users.Select(u => new UserSearchDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                AvatarUrl = u.AvatarPath,
                TotalPoints = u.TotalPoints,
                CountryName = u.Country?.Name,
                IsAlreadyFriend = false, // Ya filtramos los amigos en el repository
                HasPendingRequest = receivedRequestUserIds.Contains(u.Id) // Si el usuario ya envió solicitud a este usuario
            }).ToList();
        }

        public async Task<FriendshipDto> SendFriendRequestAsync(string currentUserId, string targetUserId)
        {
            // Validar que no sea el mismo usuario
            if (currentUserId == targetUserId)
            {
                throw new Exception("No puedes enviarte una solicitud a ti mismo");
            }

            // Obtener resumen actual
            var (friends, sentRequests, receivedRequests, me) = await _friendshipRepository.GetFriendshipSummaryAsync(currentUserId);

            // Validar límite de amigos + solicitudes enviadas
            if (friends.Count + sentRequests.Count >= MaxFriends)
            {
                throw new Exception($"Has alcanzado el límite máximo de {MaxFriends} amigos/solicitudes");
            }

            // Verificar si ya existe una solicitud (en cualquier dirección)
            var existingFriendship = await _friendshipRepository.GetFriendshipAsync(currentUserId.ToString(), targetUserId.ToString());
            var reverseFriendship = await _friendshipRepository.GetFriendshipAsync(targetUserId.ToString(), currentUserId.ToString());

            // Si el target ya envió solicitud al current, aceptar automáticamente
            if (reverseFriendship != null && reverseFriendship.Status == FriendshipStatus.Pending)
            {
                await _friendshipRepository.AcceptFriendRequestAsync(reverseFriendship.Id);
                
                // Recargar la amistad
                var acceptedFriendship = await _friendshipRepository.GetFriendshipAsync(currentUserId.ToString(), targetUserId.ToString());
                if (acceptedFriendship == null)
                {
                    acceptedFriendship = reverseFriendship;
                }
                
                return MapToDto(acceptedFriendship, currentUserId.ToString());
            }

            // Si ya hay una solicitud pendiente en la dirección correcta, lanzar error
            if (existingFriendship != null && existingFriendship.Status == FriendshipStatus.Pending)
            {
                throw new Exception("Ya enviaste una solicitud a este usuario");
            }

            // Si ya son amigos, lanzar error
            if (existingFriendship != null && existingFriendship.Status == FriendshipStatus.Accepted)
            {
                throw new Exception("Ya eres amigo de este usuario");
            }

            // Crear nueva solicitud
            var newFriendship = await _friendshipRepository.CreateFriendRequestAsync(currentUserId.ToString(), targetUserId.ToString());
            return MapToDto(newFriendship, currentUserId.ToString());
        }

        public async Task AcceptFriendRequestAsync(string currentUserId, Guid friendshipId)
        {
            // Verificar que la solicitud existe y pertenece al usuario actual
            var friendship = await _friendshipRepository.GetFriendshipByIdAsync(friendshipId);
            if (friendship == null)
            {
                throw new Exception("Solicitud no encontrada");
            }

            if (friendship.AddresseeId != currentUserId)
            {
                throw new Exception("No tienes permiso para aceptar esta solicitud");
            }

            // Obtener resumen actual para validar límite
            var (friends, sentRequests, receivedRequests, me) = await _friendshipRepository.GetFriendshipSummaryAsync(currentUserId);

            if (friends.Count >= MaxFriends)
            {
                throw new Exception($"Has alcanzado el límite máximo de {MaxFriends} amigos");
            }

            await _friendshipRepository.AcceptFriendRequestAsync(friendshipId);
        }

        public async Task DeclineFriendRequestAsync(string currentUserId, Guid friendshipId)
        {
            // Verificar que la solicitud existe y pertenece al usuario actual
            var friendship = await _friendshipRepository.GetFriendshipByIdAsync(friendshipId);
            if (friendship == null)
            {
                throw new Exception("Solicitud no encontrada");
            }

            if (friendship.AddresseeId != currentUserId)
            {
                throw new Exception("No tienes permiso para rechazar esta solicitud");
            }

            await _friendshipRepository.DeclineFriendRequestAsync(friendshipId);
        }

        public async Task RemoveFriendAsync(Guid friendshipId)
        {
            await _friendshipRepository.RemoveFriendAsync(friendshipId);
        }


        private FriendshipDto MapToDto(Friendship friendship, string currentUserId)
        {
            // Determinar quién es el amigo (el otro usuario)
            var isRequester = friendship.RequesterId == currentUserId;
            var friend = isRequester ? friendship.Addressee : friendship.Requester;

            return new FriendshipDto
            {
                Id = friendship.Id,
                FriendId = friend?.Id ?? string.Empty,
                FriendEmail = friend?.Email ?? string.Empty,
                FriendFullName = friend?.FullName ?? string.Empty,
                FriendAvatarUrl = friend?.AvatarPath,
                FriendTotalPoints = friend?.TotalPoints,
                FriendCountryName = friend?.Country?.Name,
                Status = (FriendshipStatusDto)friendship.Status,
                CreatedAt = friendship.CreatedAt
            };
        }
    }
}
