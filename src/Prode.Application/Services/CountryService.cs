using Microsoft.AspNetCore.Http;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using System.Linq.Expressions;

namespace Prode.Application.Services
{
    public class CountryService : ICountryService
    {
        public async Task<IEnumerable<Country>> GetAllAsync()
        {
            return await _countryRepository.GetAllAsync();
        }
        private readonly ICountryRepository _countryRepository;
        private readonly IFileService _fileService;

        public CountryService(ICountryRepository countryRepository, IFileService fileService)
        {
            _countryRepository = countryRepository;
            _fileService = fileService;
        }

        public async Task<IEnumerable<Country>> GetActiveCountriesAsync()
        {
            return await _countryRepository.GetActiveCountriesAsync();
        }

        public async Task<Country?> GetCountryByIdAsync(Guid id)
        {
            return await _countryRepository.GetCountryByIdAsync(id);
        }

        public async Task<PaginatedResponseDto<CountryDto>> GetCountriesAsync(CountryFilterDto filter)
        {
            // Construir la expresión de búsqueda
            Expression<Func<Country, bool>> searchExpression = null;
            if (!string.IsNullOrEmpty(filter.Search))
            {
                var searchLower = filter.Search.ToLower();
                searchExpression = c => c.Name.ToLower().Contains(searchLower) ||
                                      c.IsoCode.ToLower().Contains(searchLower) ||
                                      c.IsoCode2.ToLower().Contains(searchLower);
            }

            // Construir la expresión de ordenamiento
            Expression<Func<Country, object>> orderByExpression = filter.OrderBy?.ToLower() switch
            {
                "isocode" => c => c.IsoCode,
                "isocode2" => c => c.IsoCode2,
                _ => c => c.Name // Default order by Name
            };

            // Obtener los países con paginación y filtrado
            var countries = await _countryRepository.GetCountriesAsync(
                filter.PageNumber,
                filter.PageSize,
                searchExpression,
                orderByExpression,
                filter.OrderDescending
            );

            // Mapear a DTOs
            var countryDtos = countries.Select(c => new CountryDto
            {
                Id = c.Id,
                Name = c.Name,
                IsoCode = c.IsoCode,
                IsoCode2 = c.IsoCode2,
                FlagUrl = c.FlagUrl
            }).ToList();

            // Calcular el total de registros para la paginación
            var totalCount = await _countryRepository.GetCountriesCountAsync(searchExpression);

            return new PaginatedResponseDto<CountryDto>
            {
                Items = countryDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            };
        }

        public async Task<CountryDto> CreateCountryAsync(CountryCreateDto createDto, IFormFile? flagImage = null)
        {
            string? flagUrl = null;

            // Procesar la imagen de la bandera si se proporciona
            if (flagImage != null && flagImage.Length > 0)
            {
                using (var stream = flagImage.OpenReadStream())
                {
                    flagUrl = await _fileService.SaveFlagAsync(stream, flagImage.FileName);
                }
            }

            var country = new Country
            {
                Name = createDto.Name,
                FlagUrl = flagUrl,
                IsoCode = createDto.IsoCode,
                IsoCode2 = createDto.IsoCode2,
                IsActive = true
            };

            var createdCountry = await _countryRepository.CreateCountryAsync(country);

            return new CountryDto
            {
                Id = createdCountry.Id,
                Name = createdCountry.Name,
                IsoCode = createdCountry.IsoCode,
                IsoCode2 = createdCountry.IsoCode2,
                FlagUrl = createdCountry.FlagUrl
            };
        }

        public async Task<CountryDto> UpdateCountryAsync(Guid id, CountryUpdateDto updateDto, IFormFile? flagImage = null)
        {
            var existingCountry = await _countryRepository.GetCountryByIdAsync(id);
            if (existingCountry == null)
            {
                throw new Exception("País no encontrado.");
            }

            // Procesar nueva imagen de bandera si se proporciona
            if (flagImage != null && flagImage.Length > 0)
            {
                // Eliminar la bandera anterior si existe
                if (!string.IsNullOrEmpty(existingCountry.FlagUrl))
                {
                    var fileName = Path.GetFileName(existingCountry.FlagUrl);
                    await _fileService.DeleteAvatarAsync(fileName); // Reutilizamos el método de eliminación
                }

                // Guardar nueva bandera
                using (var stream = flagImage.OpenReadStream())
                {
                    existingCountry.FlagUrl = await _fileService.SaveFlagAsync(stream, flagImage.FileName);
                }
            }
            // Si no se proporciona imagen pero sí una URL explícita en el DTO, usarla
            else if (!string.IsNullOrEmpty(updateDto.FlagUrl))
            {
                existingCountry.FlagUrl = updateDto.FlagUrl;
            }

            if (!string.IsNullOrEmpty(updateDto.Name))
                existingCountry.Name = updateDto.Name;
            if (!string.IsNullOrEmpty(updateDto.IsoCode))
                existingCountry.IsoCode = updateDto.IsoCode;
            if (!string.IsNullOrEmpty(updateDto.IsoCode2))
                existingCountry.IsoCode2 = updateDto.IsoCode2;

            var updatedCountry = await _countryRepository.UpdateCountryAsync(existingCountry);

            return new CountryDto
            {
                Id = updatedCountry.Id,
                Name = updatedCountry.Name,
                IsoCode = updatedCountry.IsoCode,
                IsoCode2 = updatedCountry.IsoCode2,
                FlagUrl = updatedCountry.FlagUrl
            };
        }

        public async Task<bool> DeleteCountryAsync(Guid id)
        {
            return await _countryRepository.DeleteCountryAsync(id);
        }
    }
}
