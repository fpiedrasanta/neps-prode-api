namespace Prode.Application.Interfaces;

public interface IPushNotificationService
{
    /// <summary>
    /// Envía una notificación push a una suscripción de navegador
    /// </summary>
    Task SendNotificationAsync(PushSubscription subscription, string title, string body, object? data = null);

    /// <summary>
    /// Obtiene la clave pública VAPID para el frontend
    /// </summary>
    string GetVapidPublicKey();

    /// <summary>
    /// Registra una nueva suscripción de notificaciones push para un usuario
    /// </summary>
    Task SubscribeAsync(PushSubscription subscription, string userId, string userAgent);

    /// <summary>
    /// Elimina una suscripción de notificaciones push existente
    /// </summary>
    Task UnsubscribeAsync(PushSubscription subscription);

    /// <summary>
    /// Verifica si un usuario ya esta suscrito a un endpoint
    /// </summary>
    Task<bool> IsSubscribedAsync(string userId, string endpoint);
}

/// <summary>
/// Modelo de suscripción Web Push que envía el navegador
/// </summary>
public class PushSubscription
{
    public string Endpoint { get; set; } = string.Empty;
    public PushSubscriptionKeys Keys { get; set; } = new();
}

public class PushSubscriptionKeys
{
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}