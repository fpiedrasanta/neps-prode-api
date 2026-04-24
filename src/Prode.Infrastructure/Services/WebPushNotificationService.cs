using Microsoft.Extensions.Configuration;
using Prode.Application.Interfaces;
using WebPush;

namespace Prode.Infrastructure.Services;

public class WebPushNotificationService : IPushNotificationService
{
    private readonly WebPushClient _webPushClient;
    private readonly string _vapidPublicKey;
    private readonly string _vapidPrivateKey;
    private readonly string _vapidSubject;

    public WebPushNotificationService(IConfiguration configuration)
    {
        _vapidPublicKey = configuration["WebPush:VapidPublicKey"] ?? throw new InvalidOperationException("WebPush VapidPublicKey no configurada");
        _vapidPrivateKey = configuration["WebPush:VapidPrivateKey"] ?? throw new InvalidOperationException("WebPush VapidPrivateKey no configurada");
        _vapidSubject = configuration["WebPush:VapidSubject"] ?? "mailto:notificaciones@tudominio.com";

        _webPushClient = new WebPushClient();
        _webPushClient.SetVapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);
    }

    public async Task SendNotificationAsync(Prode.Application.Interfaces.PushSubscription subscription, string title, string body, object? data = null)
    {
        var payload = new
        {
            notification = new
            {
                title,
                body,
                icon = "/icons/icon-192x192.png",
                badge = "/icons/badge-72x72.png",
                data = data ?? new { }
            }
        };

        var pushSubscription = new WebPush.PushSubscription(
            endpoint: subscription.Endpoint,
            p256dh: subscription.Keys.P256dh,
            auth: subscription.Keys.Auth
        );

        await _webPushClient.SendNotificationAsync(pushSubscription, System.Text.Json.JsonSerializer.Serialize(payload));
    }

    public string GetVapidPublicKey()
    {
        return _vapidPublicKey;
    }
}