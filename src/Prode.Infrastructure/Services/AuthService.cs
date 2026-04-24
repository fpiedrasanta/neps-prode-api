using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Prode.Application.DTOs;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Net.Mail;
using System.Net;

namespace Prode.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ICountryRepository _countryRepository;
        private readonly IServiceProvider _serviceProvider;

        public AuthService(
            UserManager<ApplicationUser> userManager, 
            IConfiguration configuration, 
            ICountryRepository countryRepository,
            IServiceProvider serviceProvider)
        {
            _userManager = userManager;
            _configuration = configuration;
            _countryRepository = countryRepository;
            _serviceProvider = serviceProvider;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Check if user exists
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                throw new Exception("User already exists");
            }

            // Validate country
            var country = await _countryRepository.GetCountryByIdAsync(dto.CountryId);
            if (country == null)
            {
                throw new Exception("Country not found");
            }

            // Create ApplicationUser
            var user = new ApplicationUser
            {
                Email = dto.Email,
                UserName = dto.Email,
                FullName = dto.FullName,
                AvatarPath = null,
                Country = country,
                EmailVerificationCode = null,
                EmailVerificationCodeExpiry = null
            };

            // Create user
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to create user");
            }

            // Generar código de verificación de correo
            var verificationCode = GenerateVerificationCode();
            var expirationMinutes = int.TryParse(_configuration["EmailVerification:CodeExpirationMinutes"], out var minutes) ? minutes : 30;
            
            user.EmailVerificationCode = verificationCode;
            user.EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(expirationMinutes);
            user.IsEmailVerified = false;
            
            await _userManager.UpdateAsync(user);

            // Enviar código por correo
            await SendEmailVerificationCodeAsync(user.Email, verificationCode);

            // NO devolver token aún, el usuario necesita validar su correo primero
            return new AuthResponseDto
            {
                Token = null,
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarPath,
                CountryId = user.Country?.Id ?? Guid.Empty,
                CountryDescription = user.Country?.Name ?? "Desconocido",
                Roles = await _userManager.GetRolesAsync(user),
                RequiresEmailVerification = true
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // Find user by email with Country included
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                throw new Exception("Invalid credentials");
            }

            // Load the Country navigation property
            if (user.Country == null)
            {
                // We need to reload the user with the Country navigation property
                // Use a different approach that works with mocked UserManager in tests
                var userWithCountry = await _userManager.FindByIdAsync(user.Id);
                if (userWithCountry != null && userWithCountry.Country != null)
                {
                    user.Country = userWithCountry.Country;
                }
            }

            // Check password
            var isValidPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isValidPassword)
            {
                throw new Exception("Invalid credentials");
            }

            // Verificar que el correo haya sido verificado
            if (!user.IsEmailVerified)
            {
                throw new Exception("Debe verificar su correo electrónico antes de iniciar sesión");
            }

            // Generate JWT
            var token = await GenerateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarPath,
                CountryId = user.Country?.Id ?? Guid.Empty,
                CountryDescription = user.Country?.Name ?? "Desconocido",
                Roles = await _userManager.GetRolesAsync(user)
            };
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            // Obtener roles del usuario
            var roles = await _userManager.GetRolesAsync(user);
            
            // Crear lista de claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("FullName", user.FullName)
            };

            // Agregar roles como claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = _configuration["Jwt:Issuer"],       // <-- obligatorio si ValidateIssuer = true
                Audience = _configuration["Jwt:Audience"],   // <-- obligatorio si ValidateAudience = true
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<AuthResponseDto> LoginWithGoogleAsync(GoogleLoginDto dto)
        {
            try
            {
                // Validar el token de Google
                var googleUserInfo = await ValidateGoogleTokenAsync(dto.GoogleToken);
                if (googleUserInfo == null)
                {
                    throw new Exception("Invalid Google token");
                }

                // Buscar usuario por email
                var user = await _userManager.FindByEmailAsync(googleUserInfo.Email);
                
                if (user == null)
                {
                    // Crear nuevo usuario con datos de Google
                    user = new ApplicationUser
                    {
                        Email = googleUserInfo.Email,
                        UserName = googleUserInfo.Email,
                        FullName = googleUserInfo.Name,
                        AvatarPath = googleUserInfo.Picture,
                        Country = null // Podría asignarse un país por defecto o dejarlo null
                    };

                    user.IsEmailVerified = true;
                    user.EmailVerificationCode = null;
                    user.EmailVerificationCodeExpiry = null;

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        throw new Exception("Failed to create user");
                    }
                }
                else
                {
                    // Actualizar información del usuario existente si es necesario
                    if (string.IsNullOrEmpty(user.FullName) && !string.IsNullOrEmpty(googleUserInfo.Name))
                    {
                        user.FullName = googleUserInfo.Name;
                    }
                    if (string.IsNullOrEmpty(user.AvatarPath) && !string.IsNullOrEmpty(googleUserInfo.Picture))
                    {
                        user.AvatarPath = googleUserInfo.Picture;
                    }
                    
                    // ✅ Siempre marcar email como verificado cuando se loguea por Google
                    if (googleUserInfo.EmailVerified && !user.IsEmailVerified)
                    {
                        user.IsEmailVerified = true;
                        user.EmailVerificationCode = null;
                        user.EmailVerificationCodeExpiry = null;
                    }
                    
                    await _userManager.UpdateAsync(user);
                }

                // Cargar la relación Country
                if (user.Country == null)
                {
                    // Use a different approach that works with mocked UserManager in tests
                    var userWithCountry = await _userManager.FindByIdAsync(user.Id);
                    if (userWithCountry != null && userWithCountry.Country != null)
                    {
                        user.Country = userWithCountry.Country;
                    }
                }

                // Generar JWT
                var token = await GenerateJwtTokenAsync(user);

                return new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarPath,
                    CountryId = user.Country?.Id ?? Guid.Empty,
                    CountryDescription = user.Country?.Name ?? "Desconocido",
                    Roles = await _userManager.GetRolesAsync(user)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Google login failed: {ex.Message}");
            }
        }

        public async Task<GoogleUserInfo> ValidateGoogleTokenAsync(string googleToken)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", googleToken);
                
                var response = await client.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                
                // Intentar deserializar con configuración explícita
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                return System.Text.Json.JsonSerializer.Deserialize<GoogleUserInfo>(content, options);
            }
            catch (Exception ex)
            {
                // Log para depuración
                Console.WriteLine($"Error al validar Google access_token: {ex.Message}");
                return null;
            }
        }

        public class GoogleUserInfo
        {
            [JsonPropertyName("email")]
            public string Email { get; set; }
            
            [JsonPropertyName("name")]
            public string Name { get; set; }
            
            [JsonPropertyName("picture")]
            public string Picture { get; set; }
            
            [JsonPropertyName("sub")]
            public string Sub { get; set; }
            
            [JsonPropertyName("given_name")]
            public string GivenName { get; set; }
            
            [JsonPropertyName("family_name")]
            public string FamilyName { get; set; }
            
            [JsonPropertyName("email_verified")]
            public bool EmailVerified { get; set; }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            // Buscar usuario por email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            
            // Si el usuario no existe, no revelar información (seguridad)
            if (user == null)
            {
                // Simular éxito para no revelar si el email está registrado
                return;
            }

            // Generar código temporal
            var code = GenerateResetCode();
            var expirationMinutes = int.TryParse(_configuration["ForgotPassword:CodeExpirationMinutes"], out var minutes) ? minutes : 15;
            var expirationTime = DateTime.UtcNow.AddMinutes(expirationMinutes);

            // Guardar el código en el campo SecurityStamp (o usar una tabla separada)
            user.SecurityStamp = $"{code}|{expirationTime.Ticks}";
            await _userManager.UpdateAsync(user);

            // Enviar correo
            await SendForgotPasswordEmailAsync(user.Email, code);

            // No revelar si el email existe o no
            return;
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            // Buscar usuario por email
            var user = await _userManager.FindByEmailAsync(dto.Email);
            
            // Validar usuario
            if (user == null)
            {
                throw new Exception("Código inválido o expirado");
            }

            // Validar código y expiración
            if (string.IsNullOrEmpty(user.SecurityStamp))
            {
                throw new Exception("Código inválido o expirado");
            }

            var parts = user.SecurityStamp.Split('|');
            if (parts.Length != 2)
            {
                throw new Exception("Código inválido o expirado");
            }

            var storedCode = parts[0];
            var expirationTicks = long.Parse(parts[1]);
            var expirationTime = new DateTime(expirationTicks);

            // Validar código
            if (storedCode != dto.Code)
            {
                throw new Exception("Código inválido o expirado");
            }

            // Validar expiración
            if (DateTime.UtcNow > expirationTime)
            {
                throw new Exception("Código inválido o expirado");
            }

            // Validar contraseña
            ValidatePassword(dto.NewPassword);

            // Generar token de restablecimiento de contraseña válido
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            // Cambiar contraseña usando el token generado
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Error al cambiar la contraseña: {errors}");
            }

            // Limpiar el código temporal después del cambio exitoso
            // No establecer SecurityStamp en null, simplemente limpiar el código
            user.SecurityStamp = string.Empty;
            await _userManager.UpdateAsync(user);
        }

        private void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new Exception("La contraseña no puede estar vacía");
            }

            if (password.Length < 6)
            {
                throw new Exception("La contraseña debe tener al menos 6 caracteres");
            }

            // Puedes agregar más validaciones según las reglas de tu aplicación
            // Por ejemplo: mayúsculas, minúsculas, números, caracteres especiales
        }

        private string GenerateResetCode()
        {
            // Generar código de 6 dígitos
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        
        private string GenerateVerificationCode()
        {
            // Generar código de 6 dígitos para verificación de correo
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
        
        private async Task SendEmailVerificationCodeAsync(string email, string code)
        {
            try
            {
                var smtpConfig = _configuration.GetSection("EmailVerification:Smtp");
                
                // Verificar si la configuración SMTP está disponible
                if (smtpConfig == null || string.IsNullOrEmpty(smtpConfig["Host"]))
                {
                    // En entornos de desarrollo o pruebas, simplemente registrar el código
                    Console.WriteLine($"Código de verificación para {email}: {code}");
                    return;
                }
                
                var subject = "Verificación de Correo - Prode App";
                var body = $@"
                    <html>
                    <body>
                        <h2>Verificación de Correo Electrónico</h2>
                        <p>Hola,</p>
                        <p>Gracias por registrarte en Prode App. Tu código de verificación es:</p>
                        <h3 style='color: #28a745;'>{code}</h3>
                        <p>Este código expirará en {_configuration["EmailVerification:CodeExpirationMinutes"] ?? "30"} minutos.</p>
                        <p>Para activar tu cuenta ingresa este código en la aplicación.</p>
                        <p>Si no solicitaste este registro, por favor ignora este correo.</p>
                        <p>Saludos,<br>Equipo de Prode App</p>
                    </body>
                    </html>";

                using var client = new SmtpClient(smtpConfig["Host"], int.Parse(smtpConfig["Port"]));
                client.EnableSsl = bool.TryParse(smtpConfig["EnableSsl"], out var enableSsl) ? enableSsl : false;
                client.Credentials = new NetworkCredential(smtpConfig["Username"], smtpConfig["Password"]);

                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(smtpConfig["FromEmail"], "Prode App");
                mailMessage.To.Add(email);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log para depuración
                Console.WriteLine($"Error al enviar correo de verificación: {ex.Message}");
                // No lanzar excepción para no revelar información
            }
        }
        
        public async Task<AuthResponseDto> VerifyEmailCodeAsync(string email, string code)
        {
            // Buscar usuario por email
            var user = await _userManager.FindByEmailAsync(email);
            
            // Validar usuario
            if (user == null)
            {
                throw new Exception("Código inválido o expirado");
            }

            // Validar código y expiración
            if (string.IsNullOrEmpty(user.EmailVerificationCode))
            {
                throw new Exception("Código inválido o expirado");
            }

            // Validar código
            if (user.EmailVerificationCode != code)
            {
                throw new Exception("Código inválido o expirado");
            }

            // Validar expiración
            if (DateTime.UtcNow > user.EmailVerificationCodeExpiry)
            {
                throw new Exception("Código inválido o expirado");
            }

            // Marcar correo como verificado
            user.IsEmailVerified = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationCodeExpiry = null;
            
            await _userManager.UpdateAsync(user);

            // Generar JWT ahora que el correo esta verificado
            var token = await GenerateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarPath,
                CountryId = user.Country?.Id ?? Guid.Empty,
                CountryDescription = user.Country?.Name ?? "Desconocido",
                Roles = await _userManager.GetRolesAsync(user),
                RequiresEmailVerification = false
            };
        }

        private async Task SendForgotPasswordEmailAsync(string email, string code)
        {
            try
            {
                var smtpConfig = _configuration.GetSection("ForgotPassword:Smtp");
                
                // Verificar si la configuración SMTP está disponible
                if (smtpConfig == null || string.IsNullOrEmpty(smtpConfig["Host"]))
                {
                    // En entornos de desarrollo o pruebas, simplemente registrar el código
                    Console.WriteLine($"Código de recuperación para {email}: {code}");
                    return;
                }
                
                var subject = "Recuperación de Contraseña - Prode App";
                var body = $@"
                    <html>
                    <body>
                        <h2>Recuperación de Contraseña</h2>
                        <p>Hola,</p>
                        <p>Has solicitado recuperar tu contraseña. Tu código de recuperación es:</p>
                        <h3 style='color: #007bff;'>{code}</h3>
                        <p>Este código expirará en 15 minutos.</p>
                        <p>Si no solicitaste este cambio, por favor ignora este correo.</p>
                        <p>Saludos,<br>Equipo de Prode App</p>
                    </body>
                    </html>";

                using var client = new SmtpClient(smtpConfig["Host"], int.Parse(smtpConfig["Port"]));
                client.EnableSsl = bool.TryParse(smtpConfig["EnableSsl"], out var enableSsl) ? enableSsl : false;
                client.Credentials = new NetworkCredential(smtpConfig["Username"], smtpConfig["Password"]);

                using var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(smtpConfig["FromEmail"], "Prode App");
                mailMessage.To.Add(email);
                mailMessage.Subject = subject;
                mailMessage.Body = body;
                mailMessage.IsBodyHtml = true;

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log para depuración
                Console.WriteLine($"Error al enviar correo de recuperación: {ex.Message}");
                // No lanzar excepción para no revelar información
            }
        }

        /// <summary>
        /// Asigna ResultType a las predicciones sin puntos del usuario (se ejecuta al loguearse)
        /// </summary>
        private async Task AssignResultTypesToUserPredictionsAsync(string userId)
        {
            try
            {
                // Crear un scope nuevo para esta operación en segundo plano
                using var scope = _serviceProvider.CreateScope();
                var predictionRepository = scope.ServiceProvider.GetRequiredService<IPredictionRepository>();

                // Obtener predicciones sin ResultType de partidos finalizados
                var predictions = await predictionRepository.GetPredictionsWithoutResultTypeAsync(userId);
                if (predictions.Count == 0)
                {
                    return; // No hay nada que actualizar
                }

                // Obtener todos los ResultType de la BD
                var resultTypes = await predictionRepository.GetAllResultTypesAsync();

                int totalPointsToAdd = 0;

                foreach (var prediction in predictions)
                {
                    // Calcular el tipo de resultado
                    var resultTypeName = CalculateResultTypeName(
                        prediction.HomeGoals,
                        prediction.AwayGoals,
                        prediction.Match.HomeScore.Value,
                        prediction.Match.AwayScore.Value);

                    // Buscar el ResultType correspondiente
                    var resultType = resultTypes.FirstOrDefault(rt => rt.Name == resultTypeName);
                    if (resultType != null)
                    {
                        prediction.ResultType = resultType;
                        totalPointsToAdd += resultType.Points;
                        await predictionRepository.UpdatePredictionAsync(prediction);
                    }
                }

                // Actualizar TotalPoints del usuario si se actualizaron predicciones
                if (totalPointsToAdd > 0)
                {
                    await predictionRepository.UpdateUserTotalPointsAsync(userId, totalPointsToAdd);
                }
            }
            catch (Exception ex)
            {
                // Log del error pero no fallar el login
                Console.WriteLine($"Error al asignar ResultTypes al usuario {userId}: {ex.Message}");
            }
        }

        private string CalculateResultTypeName(int predictedHome, int predictedAway, int actualHome, int actualAway)
        {
            // Exacto: acertó resultado y goles exactos
            if (predictedHome == actualHome && predictedAway == actualAway)
            {
                return "Exacto";
            }

            // Determinar resultados
            bool predictedHomeWins = predictedHome > predictedAway;
            bool predictedAwayWins = predictedAway > predictedHome;
            bool predictedDraw = predictedHome == predictedAway;

            bool actualHomeWins = actualHome > actualAway;
            bool actualAwayWins = actualAway > actualHome;
            bool actualDraw = actualHome == actualAway;

            // Verificar si acertó el resultado
            bool acertóResultado = (predictedHomeWins && actualHomeWins) ||
                                  (predictedAwayWins && actualAwayWins) ||
                                  (predictedDraw && actualDraw);

            if (acertóResultado)
            {
                // Parcial Fuerte: acertó resultado y diferencia de goles
                int predictedDiff = predictedHome - predictedAway;
                int actualDiff = actualHome - actualAway;

                if (predictedDiff == actualDiff)
                {
                    return "Parcial Fuerte";
                }

                // Parcial Débil: solo acertó resultado
                return "Parcial Débil";
            }

            // Error: no acertó resultado
            return "Error";
        }
    }
}
