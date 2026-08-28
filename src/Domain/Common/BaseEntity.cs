namespace Domain.Common;

public abstract class BaseEntity : ISoftDelete
{
    private readonly List<object> _domainEvents = [];

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Domain events raised by state-changing methods. Collected by the
    /// persistence layer and dispatched (via MediatR) after a successful save.
    /// </summary>
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(object domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
