using Microsoft.EntityFrameworkCore;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Prode.Infrastructure.Data;

namespace Prode.Infrastructure.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly ApplicationDbContext _context;

        public FriendshipRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<Friendship> Friends, List<Friendship> SentRequests, List<Friendship> ReceivedRequests, ApplicationUser me)> GetFriendshipSummaryAsync(string userId)
        {
            // Amigos confirmados (donde el usuario es requester o addressee y status es Accepted)
            var friends = await _context.Friendships
                .Include(f => f.Requester).ThenInclude(r => r.Country)
                .Include(f => f.Addressee).ThenInclude(a => a.Country)
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && f.Status == FriendshipStatus.Accepted)
                .OrderByDescending(f => f.Requester.TotalPoints)
                .ThenByDescending(f => f.Addressee.TotalPoints)
                .ToListAsync();

            // Solicitudes enviadas (usuario es requester y status es Pending)
            var sentRequests = await _context.Friendships
                .Include(f => f.Addressee).ThenInclude(a => a.Country)
                .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            // Solicitudes recibidas (usuario es addressee y status es Pending)
            var receivedRequests = await _context.Friendships
                .Include(f => f.Requester).ThenInclude(r => r.Country)
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var me = await _context.Users.FindAsync(userId);

            return (friends, sentRequests, receivedRequests, me);
        }

        public async Task<List<ApplicationUser>> SearchUsersAsync(string userId, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return new List<ApplicationUser>();
            }

            var searchLower = search.ToLower();

            // Obtener IDs de usuarios que ya son amigos o tienen solicitudes pendientes con el usuario actual
            var excludedUserIds = await _context.Friendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId) && 
                           (f.Status == FriendshipStatus.Accepted || f.Status == FriendshipStatus.Pending))
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .ToListAsync();

            // Agregar el propio usuario
            excludedUserIds.Add(userId);

            // Buscar usuarios que no están en la lista de excluidos
            var users = await _context.Users
                .Include(u => u.Country)
                .Where(u => !excludedUserIds.Contains(u.Id) &&
                           (u.FullName.ToLower().Contains(searchLower) ||
                            u.Email.ToLower().Contains(searchLower)))
                .OrderByDescending(u => u.TotalPoints)
                .Take(20) // Límite de resultados
                .ToListAsync();

            return users;
        }

        public async Task<Friendship?> GetFriendshipAsync(string requesterId, string addresseeId)
        {
            return await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequesterId == requesterId && f.AddresseeId == addresseeId);
        }

        public async Task<Friendship?> GetFriendshipByIdAsync(Guid id)
        {
            return await _context.Friendships
                .Include(f => f.Requester).ThenInclude(r => r.Country)
                .Include(f => f.Addressee).ThenInclude(a => a.Country)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Friendship> CreateFriendRequestAsync(string requesterId, string addresseeId)
        {
            var friendship = new Friendship
            {
                Id = Guid.NewGuid(),
                RequesterId = requesterId,
                AddresseeId = addresseeId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return friendship;
        }

        public async Task AcceptFriendRequestAsync(Guid friendshipId)
        {
            var friendship = await _context.Friendships.FindAsync(friendshipId);
            if (friendship != null)
            {
                friendship.Status = FriendshipStatus.Accepted;
                friendship.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeclineFriendRequestAsync(Guid friendshipId)
        {
            var friendship = await _context.Friendships.FindAsync(friendshipId);
            if (friendship != null)
            {
                friendship.Status = FriendshipStatus.Declined;
                friendship.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFriendAsync(Guid friendshipId)
        {
            var friendship = await _context.Friendships.FindAsync(friendshipId);

            if (friendship != null)
            {
                _context.Friendships.Remove(friendship);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Friendship>> GetReceivedRequestsAsync(string userId)
        {
            return await _context.Friendships
                .Include(f => f.Requester).ThenInclude(r => r.Country)
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> DeleteExpiredPendingRequestsAsync(DateTime expirationDate)
        {
            var expiredRequests = await _context.Friendships
                .Where(f => f.Status == FriendshipStatus.Pending && f.CreatedAt < expirationDate)
                .ToListAsync();

            if (expiredRequests.Count > 0)
            {
                _context.Friendships.RemoveRange(expiredRequests);
                await _context.SaveChangesAsync();
            }

            return expiredRequests.Count;
        }
    }
}
