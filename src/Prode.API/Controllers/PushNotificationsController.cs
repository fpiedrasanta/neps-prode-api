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

    public PushNotificationsController(IPushNotificationService pushNotificationService, ApplicationDbContext dbContext)
    {
        _pushNotificationService = pushNotificationService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Devuelve la clave pública VAPID para que el frontend se suscriba
    /// </summary>
    [HttpGet("public-key")]
    public IActionResult GetVapidPublicKey()
    {
        return Ok(new { PublicKey = _pushNotificationService.GetVapidPublicKey() });
    }

    /// <summary>
    /// Envía una notificación push
    /// </summary>
    [HttpPost("send")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
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

        // Evitar duplicados
        var existing = await _dbContext.UserPushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == subscription.Endpoint);

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
        await _dbContext.SaveChangesAsync();
        
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