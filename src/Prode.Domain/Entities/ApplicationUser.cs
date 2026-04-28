using Microsoft.AspNetCore.Identity;

namespace Prode.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string? AvatarPath { get; set; }

        public int? TotalPoints { get; set; }

        public virtual Country? Country { get; set; }
        
        public bool IsEmailVerified { get; set; } = false;
        
        public string? EmailVerificationCode { get; set; }
        
        public DateTime? EmailVerificationCodeExpiry { get; set; }
        
        [Obsolete("Usar la tabla RefreshTokens en su lugar. Mantenido solo para migracion")]
        public string? RefreshToken { get; set; }
        
        [Obsolete("Usar la tabla RefreshTokens en su lugar. Mantenido solo para migracion")]
        public DateTime RefreshTokenExpiryTime { get; set; }

        // Multiples sesiones activas (multi-dispositivo)
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
