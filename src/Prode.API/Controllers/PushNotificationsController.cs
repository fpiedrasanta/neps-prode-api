using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prode.Application.Interfaces;
using Prode.Infrastructure.Data;
using Prode.Domain.Entities;

namespace Prode.API.Controllers;

/// <summary>
/// Controlador para Notificaciones PUSH Web VAPID
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PushNotificationsController : ControllerBase
{
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ApplicationDbContext _dbContext;

    private readonly ILogger<PushNotificationsController> _logger;

    public PushNotificationsController(
        ILogger<PushNotificationsController> logger, 
        IPushNotificationService pushNotificationService, 
        ApplicationDbContext dbContext)
    {
        _pushNotificationService = pushNotificationService;
        _dbContext = dbContext;
        this._logger = logger;
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
        _logger.LogInformation("✅ Endpoint: " + subscription.Endpoint);
        _logger.LogInformation("✅ P256dh: " + subscription.Keys.P256dh);
        _logger.LogInformation("✅ Auth: " + subscription.Keys.Auth);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _logger.LogInformation("✅ userId: " + userId);
        
        if (userId == null)
            return Unauthorized();

        // Evitar duplicados
        var existing = await _dbContext.UserPushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == subscription.Endpoint);
        
        _logger.LogInformation("✅ existing: " + (existing != null ? "Si" : "No"));

        if (existing != null)
            return Ok();

        var userSubscription = new UserPushSubscription
        {
            UserId = userId,
            Endpoint = subscription.Endpoint,
            P256dh = subscription.Keys.P256dh,
            Auth = subscription.Keys.Auth,
            UserAgent = Request.Headers.UserAgent.ToString()
        };

        _dbContext.UserPushSubscriptions.Add(userSubscription);
        
        _logger.LogInformation("✅ Add");
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("✅ Save");
        return Ok();
    }

    /// <summary>
    /// Endpoint para que el frontend elimine su suscripción
    /// </summary>
    [HttpPost("unsubscribe")]
    [Authorize]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscription subscription)
    {
        _logger.LogInformation("✅ Endpoint: " + subscription.Endpoint);
        _logger.LogInformation("✅ P256dh: " + subscription.Keys.P256dh);
        _logger.LogInformation("✅ Auth: " + subscription.Keys.Auth);

        var existing = await _dbContext.UserPushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == subscription.Endpoint);

        _logger.LogInformation("✅ existing: " + (existing != null ? "Si" : "No"));

        if (existing == null)
            return Ok();

        _dbContext.UserPushSubscriptions.Remove(existing);
        _logger.LogInformation("✅ Remove");

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("✅ Save");

        return Ok();
    }
}

public class SendNotificationRequest
{
    public PushSubscription Subscription { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public object? Data { get; set; }
}