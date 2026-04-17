using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using System.Security.Claims;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PredictionsController : ControllerBase
    {
        private readonly IPredictionService _predictionService;

        public PredictionsController(IPredictionService predictionService)
        {
            _predictionService = predictionService;
        }

        /// <summary>
        /// Crear una nueva predicción para un partido
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePrediction([FromBody] PredictionCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _predictionService.CreatePredictionAsync(userId, createDto);
                return CreatedAtAction(nameof(GetPrediction), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Actualizar una predicción existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePrediction(Guid id, [FromBody] PredictionUpdateDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _predictionService.UpdatePredictionAsync(userId, id, updateDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Eliminar una predicción (DELETE real)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrediction(Guid id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _predictionService.DeletePredictionAsync(userId, id);
                if (!result)
                {
                    return NotFound("Predicción no encontrada.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Obtener una predicción por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPrediction(Guid id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _predictionService.GetPredictionByIdAsync(userId, id);
                if (result == null)
                {
                    return NotFound("Predicción no encontrada.");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Obtener predicciones sin puntos asignados de partidos finalizados
        /// </summary>
        [HttpGet("without-result")]
        public async Task<IActionResult> GetPredictionsWithoutResultType()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _predictionService.GetPredictionsWithoutResultTypeAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Asignar puntos a predicciones sin resultado de partidos finalizados
        /// </summary>
        [HttpPost("assign-results")]
        public async Task<IActionResult> AssignResultTypesToPredictions()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var updatedCount = await _predictionService.AssignResultTypesToPredictionsAsync(userId);
                return Ok(new { Message = $"Se actualizaron {updatedCount} predicciones." });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
