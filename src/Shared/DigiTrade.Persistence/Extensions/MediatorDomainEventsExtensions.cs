using DigiTrade.SharedKernel.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DigiTrade.Persistence.Extensions;

public static class MediatorDomainEventsExtensions
{
    public static async Task DispatchDomainEventsAsync(this IMediator mediator, DbContext dbContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(dbContext);

        var trackedEntities = dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .ToArray();

        if (trackedEntities.Length == 0)
        {
            return;
        }

        var domainEvents = trackedEntities
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToArray();

        foreach (var trackedEntity in trackedEntities)
        {
            trackedEntity.Entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken);
        }
    }
}