using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly ICountryService _countryService;

        public CountriesController(ICountryService countryService)
        {
            _countryService = countryService;
        }

        /// <summary>
        /// Obtener países con paginación, búsqueda y ordenamiento (usuarios logueados)
        /// </summary>
        /*[Authorize]*/
        [HttpGet]
        public async Task<IActionResult> GetCountries(
            [FromQuery] string? search = null,
            [FromQuery] string? orderBy = "Name",
            [FromQuery] bool orderDescending = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var filter = new CountryFilterDto
            {
                Search = search,
                OrderBy = orderBy,
                OrderDescending = orderDescending,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _countryService.GetCountriesAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Obtener TODOS los paises sin paginar, ordenados ASC por nombre
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllCountries()
        {
            var countries = await _countryService.GetAllAsync();
            
            var result = countries
                .OrderBy(c => c.Name)
                .Select(c => new CountryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsoCode = c.IsoCode,
                    IsoCode2 = c.IsoCode2,
                    FlagUrl = c.FlagUrl
                })
                .ToList();

            return Ok(result);
        }

        /// <summary>
        /// Obtener país por ID (usuarios logueados)
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCountry(Guid id)
        {
            var country = await _countryService.GetCountryByIdAsync(id);

            if (country == null)
            {
                return NotFound("País no encontrado.");
            }

            var countryDto = new CountryDto
            {
                Id = country.Id,
                Name = country.Name,
                IsoCode = country.IsoCode,
                IsoCode2 = country.IsoCode2,
                FlagUrl = country.FlagUrl
            };

            return Ok(countryDto);
        }

        /// <summary>
        /// Crear un nuevo país (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCountry([FromForm] string name, IFormFile? flagImage, [FromForm] string? isoCode, [FromForm] string? isoCode2)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("El nombre del país es obligatorio.");
            }

            var createDto = new CountryCreateDto
            {
                Name = name,
                FlagUrl = null,
                IsoCode = isoCode,
                IsoCode2 = isoCode2
            };

            var result = await _countryService.CreateCountryAsync(createDto, flagImage);
            return CreatedAtAction(nameof(GetCountry), new { id = result.Id }, result);
        }

        /// <summary>
        /// Actualizar un país existente (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCountry(Guid id, [FromForm] string? name, IFormFile? flagImage, [FromForm] string? isoCode, [FromForm] string? isoCode2)
        {
            var updateDto = new CountryUpdateDto
            {
                Name = name,
                FlagUrl = null, // La bandera se maneja por separado
                IsoCode = isoCode,
                IsoCode2 = isoCode2
            };

            try
            {
                var result = await _countryService.UpdateCountryAsync(id, updateDto, flagImage);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Eliminar lógicamente un país (eliminado suave) (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCountry(Guid id)
        {
            var result = await _countryService.DeleteCountryAsync(id);
            
            if (!result)
            {
                return NotFound("País no encontrado.");
            }

            return NoContent();
        }
    }
}
