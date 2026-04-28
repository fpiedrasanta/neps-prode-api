using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prode.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Hash SHA256 del refresh token. NUNCA se guarda el token en texto plano
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string TokenHash { get; set; } = string.Empty;
        
        public DateTime ExpirationDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Fecha en que fue revocado manualmente o por rotacion
        /// </summary>
        public DateTime? RevokedAt { get; set; }
        
        /// <summary>
        /// Id del token que reemplazo a este (para seguimiento de cadena de rotacion)
        /// </summary>
        public Guid? ReplacedByTokenId { get; set; }
        
        [MaxLength(45)]
        public string? CreatedByIp { get; set; }
        
        [MaxLength(1024)]
        public string? UserAgent { get; set; }

        // Navegacion
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Indica si el token sigue siendo valido
        /// </summary>
        public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpirationDate;
    }
}