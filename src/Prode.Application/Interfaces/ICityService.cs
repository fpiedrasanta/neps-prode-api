using Prode.Application.DTOs;
using Prode.Domain.Entities;

namespace Prode.Application.Interfaces
{
    public interface ICityService
    {
        Task<PaginatedResponseDto<CityDto>> GetCitiesByCountryAsync(CityFilterDto filter);
        Task<CityDto> CreateCityAsync(CityCreateDto createDto);
        Task<CityDto> UpdateCityAsync(Guid id, CityUpdateDto updateDto);
        Task<bool> DeleteCityAsync(Guid id);
    }
}