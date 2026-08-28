namespace Application.Common.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<object> domainEvents, CancellationToken cancellationToken = default);
}