using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendshipsController : ControllerBase
    {
        private readonly IFriendshipService _friendshipService;

        public FriendshipsController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new Exception("Usuario no autenticado");
        }

        /// <summary>
        /// Obtener resumen de amistades del usuario actual
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetFriendshipSummary()
        {
            try
            {
                var userId = GetUserId();
                var result = await _friendshipService.GetFriendshipSummaryAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Buscar usuarios para agregar como amigos (excluye amigos actuales y solicitudes pendientes)
        /// </summary>
        /// <param name="search">Término de búsqueda (nombre o email)</param>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string search)
        {
            try
            {
                var userId = GetUserId();
                var result = await _friendshipService.SearchUsersAsync(userId, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Enviar solicitud de amistad a otro usuario
        /// </summary>
        /// <param name="targetUserId">ID del usuario al que se le envía la solicitud</param>
        [HttpPost("request")]
        public async Task<IActionResult> SendFriendRequest([FromQuery] string targetUserId)
        {
            try
            {
                var currentUserId = GetUserId();
                var result = await _friendshipService.SendFriendRequestAsync(currentUserId, targetUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Aceptar solicitud de amistad recibida
        /// </summary>
        /// <param name="friendshipId">ID de la solicitud de amistad</param>
        [HttpPost("{friendshipId}/accept")]
        public async Task<IActionResult> AcceptFriendRequest(Guid friendshipId)
        {
            try
            {
                var currentUserId = GetUserId();
                await _friendshipService.AcceptFriendRequestAsync(currentUserId, friendshipId);
                return Ok(new { message = "Solicitud aceptada exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Rechazar solicitud de amistad recibida
        /// </summary>
        /// <param name="friendshipId">ID de la solicitud de amistad</param>
        [HttpPost("{friendshipId}/decline")]
        public async Task<IActionResult> DeclineFriendRequest(Guid friendshipId)
        {
            try
            {
                var currentUserId = GetUserId();
                await _friendshipService.DeclineFriendRequestAsync(currentUserId, friendshipId);
                return Ok(new { message = "Solicitud rechazada exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Eliminar un amigo
        /// </summary>
        /// <param name="friendId">ID del amigo a eliminar</param>
        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(Guid friendId)
        {
            try
            {
                await _friendshipService.RemoveFriendAsync(friendId);
                return Ok(new { message = "Amigo eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}