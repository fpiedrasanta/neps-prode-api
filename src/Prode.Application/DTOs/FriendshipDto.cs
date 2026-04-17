namespace Prode.Application.DTOs
{
    public class FriendshipDto
    {
        public Guid Id { get; set; }
        public string FriendId { get; set; }
        public string FriendEmail { get; set; } = string.Empty;
        public string FriendFullName { get; set; } = string.Empty;
        public string? FriendAvatarUrl { get; set; }
        public int? FriendTotalPoints { get; set; }
        public string? FriendCountryName { get; set; }
        public FriendshipStatusDto Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum FriendshipStatusDto
    {
        Pending,    // Solicitud enviada, esperando respuesta
        Accepted,   // Amigos confirmados
        Declined,   // Solicitud rechazada
        Blocked     // Usuario bloqueado
    }

    public class FriendshipSummaryDto
    {
        public List<FriendshipDto> Friends { get; set; } = new();
        public List<FriendshipDto> SentRequests { get; set; } = new();
        public List<FriendshipDto> ReceivedRequests { get; set; } = new();
        public object CurrentUser { get; set; } = new();
        public int MaxFriends { get; set; } = 5;
        public int FriendsCount => Friends.Count;
        public int SentRequestsCount => SentRequests.Count;
        public int AvailableSlots => MaxFriends - FriendsCount - SentRequestsCount;
    }

    public class UserSearchDto
    {
        public string Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int? TotalPoints { get; set; }
        public string? CountryName { get; set; }
        public bool IsAlreadyFriend { get; set; }
        public bool HasPendingRequest { get; set; }
    }
}