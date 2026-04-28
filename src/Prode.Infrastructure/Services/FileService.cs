using Microsoft.Extensions.Configuration;
using Prode.Application.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Prode.Infrastructure.Services
{
    public class FileService : IFileService
    {
        private readonly string _avatarsPath;
        private readonly string _flagsPath;
        //private readonly string _baseUrl;

        public FileService(IConfiguration configuration)
        {
            //_baseUrl = configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
            
            // Crear carpeta de avatares si no existe
            _avatarsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
            if (!Directory.Exists(_avatarsPath))
            {
                Directory.CreateDirectory(_avatarsPath);
            }

            // Crear carpeta de banderas si no existe
            _flagsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "flags");
            if (!Directory.Exists(_flagsPath))
            {
                Directory.CreateDirectory(_flagsPath);
            }
        }

        public async Task<string> SaveAvatarAsync(Stream fileStream, string fileName, string userId)
        {
            try
            {
                // Validar tamaño del archivo (máximo 2MB)
                if (fileStream.Length > 2 * 1024 * 1024)
                {
                    throw new ArgumentException("El archivo es demasiado grande. El tamaño máximo es 2MB.");
                }

                // Validar tipo de archivo
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(fileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException("Formato de archivo no permitido. Solo se permiten: JPG, JPEG, PNG, GIF.");
                }

                // Generar nombre único para el archivo
                var uniqueFileName = GenerateFileName(fileName, userId);
                var filePath = Path.Combine(_avatarsPath, uniqueFileName);

                // Guardar el archivo
                using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }

                // Retornar la URL relativa del archivo
                return $"/uploads/avatars/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar el avatar: {ex.Message}");
            }
        }

        /*public Task<string> GetAvatarUrl(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return Task.FromResult("/uploads/avatars/default-avatar.png");
            }

            return Task.FromResult($"{_baseUrl}/uploads/avatars/{fileName}");
        }*/

        public async Task<bool> DeleteAvatarAsync(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return false;
                }

                var filePath = Path.Combine(_avatarsPath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string GenerateFileName(string originalFileName, string userId)
        {
            // Sanitizar el userId para evitar problemas con caracteres no válidos en nombres de archivo
            var sanitizedUserId = SanitizeFileName(userId);
            var fileExtension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            
            // Generar nombre único: userId_timestamp_random.ext
            return $"{sanitizedUserId}_{timestamp}_{random}{fileExtension}";
        }

        private string SanitizeFileName(string fileName)
        {
            // Reemplazar caracteres no válidos para nombres de archivo
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            
            // Limitar longitud máxima
            if (sanitized.Length > 100)
            {
                sanitized = sanitized.Substring(0, 100);
            }
            
            // Si queda vacío, usar un valor por defecto
            if (string.IsNullOrEmpty(sanitized))
            {
                sanitized = "user";
            }
            
            return sanitized;
        }

        public async Task<string> SaveFlagAsync(Stream fileStream, string fileName)
        {
            try
            {
                // Validar tamaño del archivo (máximo 2MB)
                if (fileStream.Length > 2 * 1024 * 1024)
                {
                    throw new ArgumentException("El archivo es demasiado grande. El tamaño máximo es 2MB.");
                }

                // Validar tipo de archivo
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
                var fileExtension = Path.GetExtension(fileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    throw new ArgumentException("Formato de archivo no permitido. Solo se permiten: JPG, JPEG, PNG, GIF, SVG.");
                }

                // Generar nombre único para el archivo
                var uniqueFileName = GenerateFlagFileName(fileName);
                var filePath = Path.Combine(_flagsPath, uniqueFileName);

                // Guardar el archivo
                using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }

                // Retornar la URL relativa del archivo
                return $"/uploads/flags/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar la bandera: {ex.Message}");
            }
        }

        private string GenerateFlagFileName(string originalFileName)
        {
            var fileExtension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            
            // Generar nombre único: flag_timestamp_random.ext
            return $"flag_{timestamp}_{random}{fileExtension}";
        }
    }
}
