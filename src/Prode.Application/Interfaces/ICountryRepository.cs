using Prode.Domain.Entities;
using System.Linq.Expressions;

namespace Prode.Application.Interfaces
{
    public interface ICountryRepository
    {
        Task<IEnumerable<Country>> GetAllAsync();
        Task<IEnumerable<Country>> GetActiveCountriesAsync();
        Task<Country?> GetCountryByIdAsync(Guid id);
        Task<IEnumerable<Country>> GetCountriesAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<Country, bool>>? searchExpression,
            Expression<Func<Country, object>> orderByExpression,
            bool orderDescending);
        Task<int> GetCountriesCountAsync(Expression<Func<Country, bool>>? searchExpression);
        Task<Country> CreateCountryAsync(Country country);
        Task<Country> UpdateCountryAsync(Country country);
        Task<bool> DeleteCountryAsync(Guid id);
    }
}