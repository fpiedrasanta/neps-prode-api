using Microsoft.EntityFrameworkCore;
using Prode.Domain.Entities;
using Prode.Application.Interfaces;
using Prode.Infrastructure.Data;
using System.Linq.Expressions;

namespace Prode.Infrastructure.Repositories
{
    public class CityRepository : ICityRepository
    {
        private readonly ApplicationDbContext _context;

        public CityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<City>> GetCitiesByCountryAsync(
            Guid countryId,
            int pageNumber,
            int pageSize,
            Expression<Func<City, bool>>? searchExpression,
            Expression<Func<City, object>> orderByExpression,
            bool orderDescending)
        {
            var query = _context.Cities
                .Where(c => c.CountryId == countryId && c.IsActive)
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

        public async Task<int> GetCitiesCountAsync(Guid countryId, Expression<Func<City, bool>>? searchExpression)
        {
            var query = _context.Cities
                .Where(c => c.CountryId == countryId && c.IsActive)
                .AsQueryable();

            // Aplicar filtro de búsqueda
            if (searchExpression != null)
            {
                query = query.Where(searchExpression);
            }

            return await query.CountAsync();
        }

        public async Task<City?> GetCityByIdAsync(Guid id)
        {
            return await _context.Cities
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
        }

        public async Task<City> CreateCityAsync(City city)
        {
            city.Id = Guid.NewGuid();
            city.IsActive = true;
            _context.Cities.Add(city);
            await _context.SaveChangesAsync();
            return city;
        }

        public async Task<City> UpdateCityAsync(City city)
        {
            _context.Cities.Update(city);
            await _context.SaveChangesAsync();
            return city;
        }

        public async Task<bool> DeleteCityAsync(Guid id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null)
            {
                return false;
            }

            city.IsActive = false;
            _context.Cities.Update(city);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}