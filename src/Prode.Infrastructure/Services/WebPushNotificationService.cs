using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prode.Application.Interfaces;
using Prode.Domain.Entities;
using Prode.Infrastructure.Data;
using WebPush;

namespace Prode.Infrastructure.Services;

public class WebPushNotificationService : IPushNotificationService
{
    private readonly WebPushClient _webPushClient;
    private readonly string _vapidPublicKey;
    private readonly string _vapidPrivateKey;
    private readonly string _vapidSubject;
    private readonly IUserPushSubscriptionRepository _userPushSubscriptionRepository;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(
        IConfiguration configuration,
        IUserPushSubscriptionRepository userPushSubscriptionRepository,
        ILogger<WebPushNotificationService> logger)
    {
        _vapidPublicKey = configuration["WebPush:VapidPublicKey"] ?? throw new InvalidOperationException("WebPush VapidPublicKey no configurada");
        _vapidPrivateKey = configuration["WebPush:VapidPrivateKey"] ?? throw new InvalidOperationException("WebPush VapidPrivateKey no configurada");
        _vapidSubject = configuration["WebPush:VapidSubject"] ?? "mailto:notificaciones@tudominio.com";
        _userPushSubscriptionRepository = userPushSubscriptionRepository;
        _logger = logger;

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

    public async Task SubscribeAsync(Prode.Application.Interfaces.PushSubscription subscription, string userId, string userAgent)
    {
        _logger.LogInformation("✅ Endpoint: {Endpoint}", subscription.Endpoint);
        _logger.LogInformation("✅ P256dh: {P256dh}", subscription.Keys.P256dh);
        _logger.LogInformation("✅ Auth: {Auth}", subscription.Keys.Auth);
        _logger.LogInformation("✅ userId: {UserId}", userId);

        // Evitar duplicados
        var existing = await _userPushSubscriptionRepository.GetByEndpointAsync(subscription.Endpoint);
        
        _logger.LogInformation("✅ existing: {Existing}", existing != null ? "Si" : "No");

        if (existing != null)
            return;

        var userSubscription = new UserPushSubscription
        {
            UserId = userId,
            Endpoint = subscription.Endpoint,
            P256dh = subscription.Keys.P256dh,
            Auth = subscription.Keys.Auth,
            UserAgent = userAgent
        };

        _logger.LogInformation("✅ Add");
        await _userPushSubscriptionRepository.AddAsync(userSubscription);
        _logger.LogInformation("✅ Save");
    }

    public async Task UnsubscribeAsync(Prode.Application.Interfaces.PushSubscription subscription)
    {
        _logger.LogInformation("✅ Endpoint: {Endpoint}", subscription.Endpoint);
        _logger.LogInformation("✅ P256dh: {P256dh}", subscription.Keys.P256dh);
        _logger.LogInformation("✅ Auth: {Auth}", subscription.Keys.Auth);

        var existing = await _userPushSubscriptionRepository.GetByEndpointAsync(subscription.Endpoint);

        _logger.LogInformation("✅ existing: {Existing}", existing != null ? "Si" : "No");

        if (existing == null)
            return;

        _logger.LogInformation("✅ Remove");
        await _userPushSubscriptionRepository.RemoveAsync(existing);
        _logger.LogInformation("✅ Save");
    }

    public async Task<bool> IsSubscribedAsync(string userId, string endpoint)
    {
        return await _userPushSubscriptionRepository.ExistsForUserAndEndpointAsync(userId, endpoint);
    }

}
