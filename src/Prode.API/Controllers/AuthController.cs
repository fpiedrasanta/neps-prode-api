using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Prode.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IFileService fileService, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _authService = authService;
            _fileService = fileService;
            _userManager = userManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDto registerDto, IFormFile? file = null)
        {
            try
            {
                // Validar archivo si se proporciona
                if (file != null && file.Length > 0)
                {
                    // Validar tipo de archivo
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(file.ContentType))
                    {
                        return BadRequest(new { message = "Formato de archivo no permitido. Solo se permiten: JPG, PNG, GIF." });
                    }

                    // Validar tamaño máximo (2MB)
                    if (file.Length > 2 * 1024 * 1024)
                    {
                        return BadRequest(new { message = "El archivo es demasiado grande. El tamaño máximo es 2MB." });
                    }

                    // Validar nombre de archivo
                    if (string.IsNullOrEmpty(file.FileName) || file.FileName.Length > 255)
                    {
                        return BadRequest(new { message = "Nombre de archivo inválido." });
                    }

                    using var stream = file.OpenReadStream();
                    registerDto.AvatarUrl = await _fileService.SaveAvatarAsync(stream, file.FileName, registerDto.Email);
                }
                else
                {
                    // Si no se proporciona archivo, establecer AvatarUrl como null o vacío
                    registerDto.AvatarUrl = null;
                }

                var result = await _authService.RegisterAsync(registerDto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto);
                SetRefreshTokenCookie(result.RefreshToken);
                result.RefreshToken = null;
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login/google")]
        public async Task<IActionResult> LoginGoogle([FromBody] GoogleLoginDto googleLoginDto)
        {
            try
            {
                var result = await _authService.LoginWithGoogleAsync(googleLoginDto);
                SetRefreshTokenCookie(result.RefreshToken);
                result.RefreshToken = null;
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                await _authService.ForgotPasswordAsync(forgotPasswordDto);
                return Ok(new { message = "Código enviado si el email existe" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                await _authService.ResetPasswordAsync(resetPasswordDto);
                return Ok(new { message = "Contraseña cambiada exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailCodeDto verifyDto)
        {
            try
            {
                var result = await _authService.VerifyEmailCodeAsync(verifyDto.Email, verifyDto.Code);
                SetRefreshTokenCookie(result.RefreshToken);
                result.RefreshToken = null;
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Crear un usuario Administrador (solo accesible para Admins)
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto createAdminDto)
        {
            try
            {
                // Validar que el email no exista
                var existingUser = await _userManager.FindByEmailAsync(createAdminDto.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "El email ya está registrado" });
                }

                // Crear el usuario
                var user = new ApplicationUser
                {
                    UserName = createAdminDto.Email,
                    Email = createAdminDto.Email,
                    FullName = createAdminDto.FullName,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, createAdminDto.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                // Asignar rol Admin
                await _userManager.AddToRoleAsync(user, "Admin");

                return Ok(new { message = "Administrador creado exitosamente", email = user.Email, fullName = user.FullName });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Renueva el Access Token usando un Refresh Token valido
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                if (!Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
                    return Unauthorized();

                var result = await _authService.RefreshTokenAsync(refreshToken);

                SetRefreshTokenCookie(result.RefreshToken);
                result.RefreshToken = null;

                return Ok(result);
            }
            catch (Exception ex)
            {
                Response.Cookies.Delete("refresh_token");
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cierra la sesion y revoca el refresh token actual
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            
            if (userId == null)
                return Unauthorized();

            if (Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
            {
                await _authService.RevokeRefreshTokenAsync(refreshToken, userId);
            }
            
            // Borrar cookie
            Response.Cookies.Delete("refresh_token");

            return Ok(new { message = "Sesion cerrada correctamente" });
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var isCrossSite = _configuration.GetValue<bool>("Cookies:CrossSite");
            var isHttps = HttpContext.Request.IsHttps;

            // ✅ Regla del navegador: Si SameSite=None → Secure OBLIGATORIO sí o sí
            // No es opcional, es un requisito del estándar
            var secure = isCrossSite || isHttps;
            
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = isCrossSite ? SameSiteMode.None : SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7),
                Path = "/",
                Domain = ".neps.com.ar"
            };

            Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
        }

        /// <summary>
        /// Cierra TODAS las sesiones del usuario en todos los dispositivos
        /// </summary>
        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            
            if (userId == null)
                return Unauthorized();

            // Revocar TODOS los tokens activos del usuario
            await _authService.RevokeAllUserRefreshTokensAsync(userId);
            
            Response.Cookies.Delete("refresh_token");

            return Ok(new { message = "Todas las sesiones han sido cerradas" });
        }
    }
}
