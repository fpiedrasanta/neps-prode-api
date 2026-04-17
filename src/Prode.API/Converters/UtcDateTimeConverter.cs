using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prode.API.Converters
{
    /// <summary>
    /// Serializa TODAS las fechas a formato ISO 8601 con marca UTC 'Z' obligatoriamente
    /// Normaliza cualquier DateTime a UTC antes de serializar
    /// Mantiene compatibilidad 100% con consumidores existentes
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Permitir cualquier formato de entrada, normalizar internamente
            if (reader.TryGetDateTimeOffset(out var dateTimeOffset))
            {
                return dateTimeOffset.UtcDateTime;
            }

            if (reader.TryGetDateTime(out var dateTime))
            {
                // Si viene sin zona, normalizar
                return dateTime.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
                    : dateTime.ToUniversalTime();
            }

            throw new JsonException("Fecha no válida");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // SIEMPRE serializar como UTC con 'Z' al final, formato ISO 8601 estricto
            DateTime utcDate;

            if (value.Kind == DateTimeKind.Utc)
            {
                utcDate = value;
            }
            else if (value.Kind == DateTimeKind.Local)
            {
                utcDate = value.ToUniversalTime();
            }
            else // Unspecified
            {
                // Asumir que ya es UTC (persistida correctamente en BD)
                utcDate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }

            // Formato ISO 8601 exacto: yyyy-MM-ddTHH:mm:ssZ
            writer.WriteStringValue(utcDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }
    }

    /// <summary>
    /// Converter para DateTimeOffset, también fuerza salida con 'Z' UTC
    /// </summary>
    public class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetDateTimeOffset().ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        }
    }
}