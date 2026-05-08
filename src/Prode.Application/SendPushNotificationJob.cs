using Microsoft.Extensions.Logging;
using Prode.Application.Interfaces;

namespace Prode.Application;

/// <summary>
/// Job de Hangfire para enviar notificaciones push en background.
/// Se ejecuta en un worker separado del request HTTP.
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
    /// Los parámetros son serializables por Hangfire (tipos primitivos + Dictionary).
    /// </summary>
    /// <param name="title">Título de la notificación</param>
    /// <param name="body">Cuerpo de la notificación</param>
    /// <param name="data">Datos adicionales (ej: click_action)</param>
    public async Task SendToAllAsync(string title, string body, Dictionary<string, string>? data = null)
    {
        _logger.LogInformation(
            "📨 [Hangfire] Iniciando envío de notificación push a todos los usuarios. Title: {Title}, Body: {Body}",
            title, body);

        try
        {
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