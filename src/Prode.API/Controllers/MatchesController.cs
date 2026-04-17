using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Enums;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatchesController : ControllerBase
    {
        private readonly IMatchService _matchService;

        public MatchesController(IMatchService matchService)
        {
            _matchService = matchService;
        }

        /// <summary>
        /// Obtener partidos por estado (usuarios logueados)
        /// </summary>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMatches(
            [FromQuery] MatchStatusFilter? status = null,
            [FromQuery] string? teamNameSearch = null,
            [FromQuery] int? minutesBeforeMatchToLock = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            
            var filter = new MatchFilterDto
            {
                Status = status ?? MatchStatusFilter.Upcoming,
                TeamNameSearch = teamNameSearch,
                MinutesBeforeMatchToLock = minutesBeforeMatchToLock ?? 15,
                UserId = userId?.ToString(),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _matchService.GetMatchesByStatusAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Obtener partido por ID (usuarios logueados)
        /// </summary>
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMatch(Guid id)
        {
            var result = await _matchService.GetMatchByIdAsync(id);
            if (result == null)
            {
                return NotFound("Partido no encontrado.");
            }
            return Ok(result);
        }

        /// <summary>
        /// Crear un nuevo partido (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateMatch([FromBody] MatchCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _matchService.CreateMatchAsync(createDto);
            return CreatedAtAction(nameof(GetMatch), new { id = result.Id }, result);
        }

        /// <summary>
        /// Actualizar un partido existente (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMatch(Guid id, [FromBody] MatchUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _matchService.UpdateMatchAsync(id, updateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Actualizar solo los scores de un partido (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id}/scores")]
        public async Task<IActionResult> UpdateMatchScores(Guid id, [FromBody] MatchScoreDto scoreDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _matchService.UpdateMatchScoresAsync(id, scoreDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Eliminar lógicamente un partido (eliminado suave) (solo Admin)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMatch(Guid id)
        {
            var result = await _matchService.DeleteMatchAsync(id);

            if (!result)
            {
                return NotFound("Partido no encontrado.");
            }

            return NoContent();
        }
    }
}