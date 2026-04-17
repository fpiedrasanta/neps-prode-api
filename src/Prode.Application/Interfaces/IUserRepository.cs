using Prode.Application.DTOs;
using Prode.Domain.Entities;

namespace Prode.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<(List<ApplicationUser> Users, int TotalCount, Dictionary<string, int> UserPositions)> GetRankingAsync(UserRankingFilterDto filter);
        Task<ApplicationUser> GetByIdAsync(Guid id);
        Task UpdateAsync(ApplicationUser user);
    }
}
