using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prode.Application.Interfaces;

namespace Prode.API.Controllers;

/// <summary>
/// Controlador para Notificaciones PUSH Web VAPID
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PushNotificationsController : ControllerBase
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ILogger<PushNotificationsController> _logger;

    public PushNotificationsController(
        ILogger<PushNotificationsController> logger, 
        IPushNotificationService pushNotificationService)
    {
        _pushNotificationService = pushNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Devuelve la clave pública VAPID para que el frontend se suscriba
    /// </summary>
    [HttpGet("public-key")]
    public IActionResult GetVapidPublicKey()
    {
        string publicKey = _pushNotificationService.GetVapidPublicKey();
        _logger.LogInformation("✅ Public Key: " + publicKey);
        return Ok(new { PublicKey = publicKey });
    }

    /// <summary>
    /// Envía una notificación push
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        _logger.LogInformation("✅ Subscription: " + request.Subscription);
        _logger.LogInformation("✅ Title: " + request.Title);
        _logger.LogInformation("✅ Body: " + request.Body);
        _logger.LogInformation("✅ Data: " + request.Data);

        await _pushNotificationService.SendNotificationAsync(
            request.Subscription, 
            request.Title, 
            request.Body, 
            request.Data);
        
        return Ok();
    }

    /// <summary>
    /// Endpoint para que el frontend guarde su suscripción cuando el usuario se suscribe
    /// </summary>
    [HttpPost("subscribe")]
    [Authorize]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscription subscription)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (userId == null)
            return Unauthorized();

        await _pushNotificationService.SubscribeAsync(subscription, userId, Request.Headers.UserAgent.ToString());
        
        return Ok();
    }

    /// <summary>
    /// Endpoint para que el frontend elimine su suscripción
    /// </summary>
    [HttpPost("unsubscribe")]
    [Authorize]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscription subscription)
    {
        await _pushNotificationService.UnsubscribeAsync(subscription);

        return Ok();
    }

    [HttpGet("status-by-endpoint")]
    [Authorize]
    public async Task<IActionResult> StatusByEndpoint([FromQuery] string endpoint)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var isSubscribed = await _pushNotificationService
            .IsSubscribedAsync(userId, endpoint);

        return Ok(new { isSubscribed });
    }
}

public class SendNotificationRequest
{
    public PushSubscription Subscription { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public object? Data { get; set; }
}