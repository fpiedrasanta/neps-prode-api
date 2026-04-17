using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AvatarController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AvatarController(IFileService fileService, UserManager<ApplicationUser> userManager)
        {
            _fileService = fileService;
            _userManager = userManager;
        }


        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAvatar(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("Usuario no encontrado.");
                }

                var avatarUrl = await _fileService.GetAvatarUrl(user.AvatarPath);
                return Ok(new AvatarResponseDto
                {
                    AvatarUrl = avatarUrl,
                    Message = "Avatar obtenido exitosamente."
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al obtener el avatar: {ex.Message}");
            }
        }

    }
}