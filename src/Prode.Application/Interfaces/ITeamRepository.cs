using Prode.Domain.Entities;
using System.Linq.Expressions;

namespace Prode.Application.Interfaces
{
    public interface ITeamRepository
    {
        Task<IEnumerable<Team>> GetTeamsAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<Team, bool>>? searchExpression,
            Expression<Func<Team, object>> orderByExpression,
            bool orderDescending);
        Task<IEnumerable<Team>> GetTeamsByCountryAsync(
            Guid countryId,
            int pageNumber,
            int pageSize,
            Expression<Func<Team, bool>>? searchExpression,
            Expression<Func<Team, object>> orderByExpression,
            bool orderDescending);
        Task<int> GetTeamsCountAsync(Expression<Func<Team, bool>>? searchExpression);
        Task<int> GetTeamsCountByCountryAsync(Guid countryId, Expression<Func<Team, bool>>? searchExpression);
        Task<Team?> GetTeamByIdAsync(Guid id);
        Task<Team> CreateTeamAsync(Team team);
        Task<Team> UpdateTeamAsync(Team team);
        Task<bool> DeleteTeamAsync(Guid id);
    }
}