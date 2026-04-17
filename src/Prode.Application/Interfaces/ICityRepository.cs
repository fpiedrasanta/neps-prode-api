using Prode.Domain.Entities;
using System.Linq.Expressions;

namespace Prode.Application.Interfaces
{
    public interface ICityRepository
    {
        Task<IEnumerable<City>> GetCitiesByCountryAsync(
            Guid countryId,
            int pageNumber,
            int pageSize,
            Expression<Func<City, bool>>? searchExpression,
            Expression<Func<City, object>> orderByExpression,
            bool orderDescending);
        Task<int> GetCitiesCountAsync(Guid countryId, Expression<Func<City, bool>>? searchExpression);
        Task<City?> GetCityByIdAsync(Guid id);
        Task<City> CreateCityAsync(City city);
        Task<City> UpdateCityAsync(City city);
        Task<bool> DeleteCityAsync(Guid id);
    }
}