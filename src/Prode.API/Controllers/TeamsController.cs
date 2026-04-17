using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly ITeamService _teamService;

        public TeamsController(ITeamService teamService)
        {
            _teamService = teamService;
        }

        /// <summary>
        /// Obtener todos los equipos con paginación, búsqueda y ordenamiento (usuarios logueados)
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetTeams(
            [FromQuery] string? search = null,
            [FromQuery] string? orderBy = "Name",
            [FromQuery] bool orderDescending = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var filter = new TeamFilterDto
            {
                Search = search,
                OrderBy = orderBy,
                OrderDescending = orderDescending,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _teamService.GetTeamsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Obtener equipos de un país con paginación, búsqueda y ordenamiento (usuarios logueados)
        /// </summary>
        [Authorize]
        [HttpGet("country/{countryId}")]
        public async Task<IActionResult> GetTeamsByCountry(
            Guid countryId,
            [FromQuery] string? search = null,
            [FromQuery] string? orderBy = "Name",
            [FromQuery] bool orderDescending = false,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var filter = new TeamFilterDto
            {
                CountryId = countryId,
                Search = search,
                OrderBy = orderBy,
                OrderDescending = orderDescending,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _teamService.GetTeamsByCountryAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Obtener equipo por ID (usuarios logueados)
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeam(Guid id)
        {
            var result = await _teamService.GetTeamByIdAsync(id);
            if (result == null)
            {
                return NotFound("Equipo no encontrado.");
            }
            return Ok(result);
        }

        /// <summary>
        /// Crear un nuevo equipo (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateTeam([FromForm] string name, IFormFile? flagImage, [FromForm] Guid countryId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("El nombre del equipo es obligatorio.");
            }

            var createDto = new TeamCreateDto
            {
                Name = name,
                CountryId = countryId
            };

            var result = await _teamService.CreateTeamAsync(createDto, flagImage);
            return CreatedAtAction(nameof(GetTeam), new { id = result.Id }, result);
        }

        /// <summary>
        /// Actualizar un equipo existente (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTeam(Guid id, [FromForm] string? name, IFormFile? flagImage, [FromForm] Guid? countryId)
        {
            var updateDto = new TeamUpdateDto
            {
                Name = name,
                CountryId = countryId
            };

            try
            {
                var result = await _teamService.UpdateTeamAsync(id, updateDto, flagImage);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Eliminar lógicamente un equipo (eliminado suave) (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeam(Guid id)
        {
            var result = await _teamService.DeleteTeamAsync(id);

            if (!result)
            {
                return NotFound("Equipo no encontrado.");
            }

            return NoContent();
        }
    }
}