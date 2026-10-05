using Application.Common.Interfaces;
using Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
            await DispatchSingleAsync((dynamic)domainEvent, cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchSingleAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        var handlers = _serviceProvider.GetServices<IDomainEventHandler<TEvent>>();

        foreach (var handler in handlers)
            await handler.HandleAsync(domainEvent, cancellationToken).ConfigureAwait(false);
    }
}
