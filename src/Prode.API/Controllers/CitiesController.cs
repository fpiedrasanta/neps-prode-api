using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CitiesController : ControllerBase
    {
        private readonly ICityService _cityService;

        public CitiesController(ICityService cityService)
        {
            _cityService = cityService;
        }

        /// <summary>
        /// Obtener ciudades de un país con paginación, búsqueda y ordenamiento (usuarios logueados)
        /// </summary>
        [Authorize]
        [HttpGet("country/{countryId}")]
        public async Task<IActionResult> GetCitiesByCountry(
            Guid countryId,
            [FromQuery] string? search = null,
            [FromQuery] string? orderBy = "Name",
            [FromQuery] bool orderDescending = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var filter = new CityFilterDto
            {
                CountryId = countryId,
                Search = search,
                OrderBy = orderBy,
                OrderDescending = orderDescending,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _cityService.GetCitiesByCountryAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Crear una nueva ciudad (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateCity([FromBody] CityCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _cityService.CreateCityAsync(createDto);
            return CreatedAtAction(nameof(GetCitiesByCountry), new { countryId = result.CountryId }, result);
        }

        /// <summary>
        /// Actualizar una ciudad existente (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCity(Guid id, [FromBody] CityUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _cityService.UpdateCityAsync(id, updateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Eliminar lógicamente una ciudad (eliminado suave) (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCity(Guid id)
        {
            var result = await _cityService.DeleteCityAsync(id);

            if (!result)
            {
                return NotFound("Ciudad no encontrada.");
            }

            return NoContent();
        }
    }
}