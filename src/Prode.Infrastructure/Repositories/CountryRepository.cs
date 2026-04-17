using Microsoft.EntityFrameworkCore;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Prode.Infrastructure.Data;
using System.Linq.Expressions;

namespace Prode.Infrastructure.Repositories
{
    public class CountryRepository : ICountryRepository
    {
        public async Task<IEnumerable<Country>> GetAllAsync()
        {
            return await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        private readonly ApplicationDbContext _context;

        public CountryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Country>> GetActiveCountriesAsync()
        {
            return await _context.Countries
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Country?> GetCountryByIdAsync(Guid id)
        {
            return await _context.Countries
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<IEnumerable<Country>> GetCountriesAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<Country, bool>>? searchExpression,
            Expression<Func<Country, object>> orderByExpression,
            bool orderDescending)
        {
            var query = _context.Countries
                .Where(c => c.IsActive)
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

        public async Task<int> GetCountriesCountAsync(Expression<Func<Country, bool>>? searchExpression)
        {
            var query = _context.Countries
                .Where(c => c.IsActive)
                .AsQueryable();

            // Aplicar filtro de búsqueda
            if (searchExpression != null)
            {
                query = query.Where(searchExpression);
            }

            return await query.CountAsync();
        }

        public async Task<Country> CreateCountryAsync(Country country)
        {
            country.Id = Guid.NewGuid();
            country.IsActive = true;
            _context.Countries.Add(country);
            await _context.SaveChangesAsync();
            return country;
        }

        public async Task<Country> UpdateCountryAsync(Country country)
        {
            _context.Countries.Update(country);
            await _context.SaveChangesAsync();
            return country;
        }

        public async Task<bool> DeleteCountryAsync(Guid id)
        {
            var country = await _context.Countries.FindAsync(id);
            if (country == null)
            {
                return false;
            }

            country.IsActive = false;
            _context.Countries.Update(country);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
