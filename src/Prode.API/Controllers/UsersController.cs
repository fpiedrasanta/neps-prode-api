using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }
        
        /// <summary>
        /// Obtener usuario por Id
        /// </summary>
        /// <param name="id">Id del usuario</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                
                if (user == null)
                    return NotFound($"Usuario con id {id} no encontrado");

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Actualizar usuario
        /// </summary>
        /// <param name="id">Id del usuario</param>
        /// <param name="email">Email del usuario</param>
        /// <param name="fullName">Nombre completo</param>
        /// <param name="avatar">Archivo de imagen para avatar</param>
        /// <param name="countryId">Id del país</param>
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(
            Guid id, 
            [FromForm] string email, 
            [FromForm] string fullName, 
            IFormFile? avatar, 
            [FromForm] Guid countryId)
        {
            try
            {
                var updateDto = new UserUpdateDto
                {
                    Email = email,
                    FullName = fullName,
                    CountryId = countryId
                };

                var updatedUser = await _userService.UpdateAsync(id, updateDto, avatar);
                
                if (updatedUser == null)
                    return NotFound($"Usuario con id {id} no encontrado");

                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Obtener ranking de usuarios ordenados por puntos (mayor a menor)
        /// </summary>
        /// <param name="pageNumber">Número de página (default: 1)</param>
        /// <param name="pageSize">Cantidad de usuarios por página (default: 10)</param>
        /// <param name="search">Filtrar por nombre o email</param>
        [HttpGet("ranking")]
        public async Task<IActionResult> GetRanking(
            [FromQuery] int pageNumber = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? search = null)
        {
            try
            {
                var filter = new UserRankingFilterDto
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Search = search
                };

                var result = await _userService.GetRankingAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}