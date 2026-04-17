using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using System.Linq.Expressions;

namespace Prode.Application.Services
{
    public class CityService : ICityService
    {
        private readonly ICityRepository _cityRepository;

        public CityService(ICityRepository cityRepository)
        {
            _cityRepository = cityRepository;
        }

        public async Task<PaginatedResponseDto<CityDto>> GetCitiesByCountryAsync(CityFilterDto filter)
        {
            // Construir la expresión de búsqueda
            Expression<Func<City, bool>> searchExpression = null;
            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                searchExpression = c => c.Name.ToLower().Contains(searchLower);
            }

            // Construir la expresión de ordenamiento
            Expression<Func<City, object>> orderByExpression = filter.OrderBy?.ToLower() switch
            {
                "name" => c => c.Name,
                _ => c => c.Name // Default order by Name
            };

            // Obtener las ciudades con paginación y filtrado
            var cities = await _cityRepository.GetCitiesByCountryAsync(
                filter.CountryId,
                filter.PageNumber,
                filter.PageSize,
                searchExpression,
                orderByExpression,
                filter.OrderDescending
            );

            // Mapear a DTOs
            var cityDtos = cities.Select(c => new Application.DTOs.CityDto
            {
                Id = c.Id,
                Name = c.Name,
                CountryId = c.CountryId,
                CountryName = c.Country?.Name
            }).ToList();

            // Calcular el total de registros para la paginación
            var totalCount = await _cityRepository.GetCitiesCountAsync(filter.CountryId, searchExpression);

            return new PaginatedResponseDto<CityDto>
            {
                Items = cityDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        public async Task<CityDto> CreateCityAsync(CityCreateDto createDto)
        {
            var city = new City
            {
                Name = createDto.Name,
                CountryId = createDto.CountryId,
                IsActive = true
            };

            var createdCity = await _cityRepository.CreateCityAsync(city);

            return new Application.DTOs.CityDto
            {
                Id = createdCity.Id,
                Name = createdCity.Name,
                CountryId = createdCity.CountryId,
                CountryName = createdCity.Country?.Name
            };
        }

        public async Task<CityDto> UpdateCityAsync(Guid id, CityUpdateDto updateDto)
        {
            var existingCity = await _cityRepository.GetCityByIdAsync(id);
            if (existingCity == null)
            {
                throw new Exception("Ciudad no encontrada.");
            }

            if (!string.IsNullOrEmpty(updateDto.Name))
                existingCity.Name = updateDto.Name;
            if (updateDto.CountryId.HasValue)
                existingCity.CountryId = updateDto.CountryId.Value;

            var updatedCity = await _cityRepository.UpdateCityAsync(existingCity);

            return new Application.DTOs.CityDto
            {
                Id = updatedCity.Id,
                Name = updatedCity.Name,
                CountryId = updatedCity.CountryId,
                CountryName = updatedCity.Country?.Name
            };
        }

        public async Task<bool> DeleteCityAsync(Guid id)
        {
            return await _cityRepository.DeleteCityAsync(id);
        }
    }
}