using Prode.Domain.Entities;

namespace Prode.Application.Interfaces;

public interface IUserPushSubscriptionRepository
{
    /// <summary>
    /// Busca una suscripción por su endpoint
    /// </summary>
    Task<UserPushSubscription?> GetByEndpointAsync(string endpoint);

    /// <summary>
    /// Agrega una nueva suscripción
    /// </summary>
    Task AddAsync(UserPushSubscription subscription);

    /// <summary>
    /// Elimina una suscripción existente
    /// </summary>
    Task RemoveAsync(UserPushSubscription subscription);

    /// <summary>
    /// Verifica si existe una suscripción para usuario y endpoint
    /// </summary>
    Task<bool> ExistsForUserAndEndpointAsync(string userId, string endpoint);
}
