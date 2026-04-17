using Microsoft.EntityFrameworkCore;
using Prode.Domain.Entities;
using Prode.Application.Interfaces;
using Prode.Infrastructure.Data;
using System.Linq.Expressions;

namespace Prode.Infrastructure.Repositories
{
    public class TeamRepository : ITeamRepository
    {
        private readonly ApplicationDbContext _context;

        public TeamRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Team>> GetTeamsAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<Team, bool>>? searchExpression,
            Expression<Func<Team, object>> orderByExpression,
            bool orderDescending)
        {
            var query = _context.Teams
                .Where(t => t.IsActive)
                .Include(t => t.Country)
                .AsQueryable();

            // Aplicar filtro de búsqueda
            if (searchExpression != null)
            {
                query = query.Where(searchExpression);
            }

            // Aplicar ordenamiento
            if (orderDescending)
            {
                query = query.OrderByDescending(orderByExpression);
            }
            else
            {
                query = query.OrderBy(orderByExpression);
            }

            // Aplicar paginación
            var skip = (pageNumber - 1) * pageSize;
            return await query.Skip(skip).Take(pageSize).ToListAsync();
        }

        public async Task<IEnumerable<Team>> GetTeamsByCountryAsync(
            Guid countryId,
            int pageNumber,
            int pageSize,
            Expression<Func<Team, bool>>? searchExpression,
            Expression<Func<Team, object>> orderByExpression,
            bool orderDescending)
        {
            var query = _context.Teams
                .Where(t => t.CountryId == countryId && t.IsActive)
                .Include(t => t.Country)
                .AsQueryable();

            // Aplicar filtro de búsqueda
            if (searchExpression != null)
            {
                query = query.Where(searchExpression);
            }

            // Aplicar ordenamiento
            if (orderDescending)
            {
                query = query.OrderByDescending(orderByExpression);
            }
            else
            {
                query = query.OrderBy(orderByExpression);
            }

            // Aplicar paginación
            var skip = (pageNumber - 1) * pageSize;
            return await query.Skip(skip).Take(pageSize).ToListAsync();
        }

        public async Task<int> GetTeamsCountAsync(Expression<Func<Team, bool>>? searchExpression)
        {
            var query = _context.Teams
                .Where(t => t.IsActive)
                .AsQueryable();

            // Aplicar filtro de búsqueda
            if (searchExpression != null)
            {
                query = query.Where(searchExpression);
            }

            return await query.CountAsync();
        }

        public async Task<int> GetTeamsCountByCountryAsync(Guid countryId, Expression<Func<Team, bool>>? searchExpression)
        {
            var query = _context.Teams
                .Where(t => t.CountryId == countryId && t.IsActive)
                .AsQueryable();

            // Aplicar filtro de búsqueda
            if (searchExpression != null)
            {
                query = query.Where(searchExpression);
            }

            return await query.CountAsync();
        }

        public async Task<Team?> GetTeamByIdAsync(Guid id)
        {
            return await _context.Teams
                .Include(t => t.Country)
                .FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
        }

        public async Task<Team> CreateTeamAsync(Team team)
        {
            team.Id = Guid.NewGuid();
            team.IsActive = true;
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task<Team> UpdateTeamAsync(Team team)
        {
            _context.Teams.Update(team);
            await _context.SaveChangesAsync();
            return team;
        }

        public async Task<bool> DeleteTeamAsync(Guid id)
        {
            var team = await _context.Teams.FindAsync(id);
            if (team == null)
            {
                return false;
            }

            team.IsActive = false;
            _context.Teams.Update(team);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}