using Prode.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Prode.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginDto dto);
        Task ForgotPasswordAsync(ForgotPasswordDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task<AuthResponseDto> VerifyEmailCodeAsync(string email, string code);
        
        /// <summary>
        /// Renueva el access token usando un refresh token valido (Sliding expiration)
        /// </summary>
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);

        /// <summary>
        /// Revoca el refresh token actual del usuario
        /// </summary>
        Task RevokeRefreshTokenAsync(string refreshToken, string userId);

        /// <summary>
        /// Revoca TODOS los refresh tokens activos del usuario (todos los dispositivos)
        /// </summary>
        Task RevokeAllUserRefreshTokensAsync(string userId);
    }
}
