using System;
using Microsoft.Extensions.Logging;

namespace Prode.Application.Helpers
{
    public static class DateTimeHelper
    {
        private static ILogger? _logger;

        public static void Initialize(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Normaliza cualquier fecha de entrada a UTC usando DateTimeOffset
        /// Si no tiene información de zona horaria, asume hora local del cliente (temporalmente)
        /// Loguea casos ambiguos
        /// </summary>
        public static DateTimeOffset NormalizeToUtc(DateTime dateTime, string context = "")
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return new DateTimeOffset(dateTime, TimeSpan.Zero);
            }

            if (dateTime.Kind == DateTimeKind.Local)
            {
                var offset = TimeZoneInfo.Local.GetUtcOffset(dateTime);
                var utcDate = new DateTimeOffset(dateTime, offset).ToUniversalTime();
                
                _logger?.LogWarning("Fecha recibida en hora local del SERVIDOR, convertida a UTC. Contexto: {Context}. Fecha original: {Original}", 
                    context, dateTime.ToString("o"));

                return utcDate;
            }

            // Caso ambiguo: DateTimeKind.Unspecified (proviene de string sin zona horaria)
            // Asumimos hora local del cliente (America/Argentina/Buenos_Aires UTC-3)
            var defaultOffset = TimeSpan.FromHours(-3);
            var normalizedUtc = new DateTimeOffset(dateTime, defaultOffset).ToUniversalTime();

            _logger?.LogWarning("Fecha sin información de zona horaria detectada. Asumida UTC-3 (Argentina). Contexto: {Context}. Fecha original: {Original}, Fecha UTC: {Utc}", 
                context, dateTime.ToString("o"), normalizedUtc.ToString("o"));

            return normalizedUtc;
        }

        /// <summary>
        /// Normaliza fecha para persistencia en BD: siempre UTC
        /// Devuelve DateTime para mantener compatibilidad con entidades existentes
        /// </summary>
        public static DateTime NormalizeForPersistence(DateTime dateTime, string context = "")
        {
            return NormalizeToUtc(dateTime, context).UtcDateTime;
        }

        /// <summary>
        /// Obtiene la hora actual en UTC de forma segura
        /// </summary>
        public static DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// Obtiene DateTimeOffset actual en UTC
        /// </summary>
        public static DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
    }
}