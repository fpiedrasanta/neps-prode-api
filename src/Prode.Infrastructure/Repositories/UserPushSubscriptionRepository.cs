using Microsoft.EntityFrameworkCore;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Prode.Infrastructure.Data;

namespace Prode.Infrastructure.Repositories;

public class UserPushSubscriptionRepository : IUserPushSubscriptionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserPushSubscriptionRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserPushSubscription?> GetByEndpointAsync(string endpoint)
    {
        return await _dbContext.UserPushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == endpoint);
    }

    public async Task AddAsync(UserPushSubscription subscription)
    {
        _dbContext.UserPushSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveAsync(UserPushSubscription subscription)
    {
        _dbContext.UserPushSubscriptions.Remove(subscription);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> ExistsForUserAndEndpointAsync(string userId, string endpoint)
    {
        return await _dbContext.UserPushSubscriptions
            .AnyAsync(x => x.UserId == userId && x.Endpoint == endpoint);
    }
}
