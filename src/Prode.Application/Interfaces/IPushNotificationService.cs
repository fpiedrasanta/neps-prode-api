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