namespace Domain.Exceptions
{
    public sealed class EntityNotFoundException : DomainException
    {
        public Type EntityType { get; }
        public Guid EntityId { get; }

        public EntityNotFoundException(Type entityType, Guid entityId)
            : base($"Resource '{entityType.Name}' with id '{entityId}' was not found.")
        {
            EntityType = entityType;
            EntityId = entityId;
        }
    }
}