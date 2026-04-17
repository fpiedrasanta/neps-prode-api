using Microsoft.EntityFrameworkCore;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Prode.Infrastructure.Data;

namespace Prode.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(List<ApplicationUser> Users, int TotalCount, Dictionary<string, int> UserPositions)> GetRankingAsync(UserRankingFilterDto filter)
        {
            var query = _context.Users
                .Include(u => u.Country)
                .Where(u => u.IsEmailVerified)
                .AsQueryable();

            // Aplicar filtro por nombre o email si se especifica
            if (!string.IsNullOrEmpty(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(u => 
                    u.FullName.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search));
            }

            // Obtener total antes de paginar
            var totalCount = await query.CountAsync();

            // Obtener todos los usuarios ordenados por TotalPoints (sin filtro) para calcular posiciones globales
            var allUsersOrdered = await _context.Users
                .Where(u => u.IsEmailVerified)
                .OrderByDescending(u => u.TotalPoints)
                .Select(u => new { u.Id, u.TotalPoints })
                .ToListAsync();

            // Crear diccionario de posiciones globales (1-based)
            var userPositions = new Dictionary<string, int>();
            int position = 1;
            int? previousPoints = null;
            foreach (var user in allUsersOrdered)
            {
                // Si tiene los mismos puntos que el anterior, mantiene la misma posición
                if (user.TotalPoints == previousPoints)
                {
                    userPositions[user.Id] = position - 1; // Mantiene la posición anterior
                }
                else
                {
                    userPositions[user.Id] = position;
                }
                previousPoints = user.TotalPoints;
                position++;
            }

            // Ordenar por TotalPoints descendente (mayor a menor)
            query = query.OrderByDescending(u => u.TotalPoints);

            // Aplicar paginación
            var skip = (filter.PageNumber - 1) * filter.PageSize;
            var users = await query
                .Skip(skip)
                .Take(filter.PageSize)
                .ToListAsync();

            return (users, totalCount, userPositions);
        }

        public async Task<ApplicationUser> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.Country)
                .FirstOrDefaultAsync(u => u.Id == id.ToString());
        }

        public async Task UpdateAsync(ApplicationUser user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
