using Microsoft.AspNetCore.Http;
using Prode.Application.DTOs;
using Prode.Domain.Entities;

namespace Prode.Application.Interfaces
{
    public interface ICountryService
    {
        Task<IEnumerable<Country>> GetAllAsync();
        Task<IEnumerable<Country>> GetActiveCountriesAsync();
        Task<Country?> GetCountryByIdAsync(Guid id);
        Task<PaginatedResponseDto<CountryDto>> GetCountriesAsync(CountryFilterDto filter);
        Task<CountryDto> CreateCountryAsync(CountryCreateDto createDto, IFormFile? flagImage = null);
        Task<CountryDto> UpdateCountryAsync(Guid id, CountryUpdateDto updateDto, IFormFile? flagImage = null);
        Task<bool> DeleteCountryAsync(Guid id);
    }
}
