using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prode.Application.Interfaces;

namespace Prode.Application;

/// <summary>
/// Job de Hangfire para enviar notificaciones push en background.
/// Se ejecuta en un worker separado del request HTTP.
/// Solo usa tipos primitivos como parámetros para evitar problemas de serialización con Hangfire.
/// </summary>
public class SendPushNotificationJob
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<SendPushNotificationJob> _logger;

    public SendPushNotificationJob(
        IPushNotificationService pushNotificationService,
        ILogger<SendPushNotificationJob> logger)
    {
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Envía una notificación push a TODOS los usuarios suscriptos.
    /// Solo usa string como parámetros (Hangfire serializa strings sin problemas).
    /// </summary>
    /// <param name="title">Título de la notificación</param>
    /// <param name="body">Cuerpo de la notificación</param>
    /// <param name="dataJson">JSON opcional con datos adicionales (ej: {"click_action":"/feed"})</param>
    public async Task SendToAllAsync(string title, string body, string? dataJson = null)
    {
        _logger.LogInformation(
            "📨 [Hangfire] Iniciando envío de notificación push a todos los usuarios. Title: {Title}, Body: {Body}",
            title, body);

        try
        {
            object? data = null;
            if (!string.IsNullOrEmpty(dataJson))
            {
                data = JsonSerializer.Deserialize<object>(dataJson);
            }

            await _pushNotificationService.SendNotificationToAllUsersAsync(title, body, data);

            _logger.LogInformation(
                "✅ [Hangfire] Notificación push enviada exitosamente a todos los usuarios. Title: {Title}",
                title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [Hangfire] Error al enviar notificación push a todos los usuarios. Title: {Title}",
                title);

            // Relanzar para que Hangfire reintente automáticamente
            // Solo se reintenta si es un error transitorio (DB caída, red, etc.)
            // Los errores de suscripciones inválidas/expiradas ya son manejados internamente
            // por WebPushNotificationService y NO relanzan excepción.
            throw;
        }
    }
}
