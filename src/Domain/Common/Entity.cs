namespace Domain.Common;

public abstract class Entity : IAuditable, ISoftDelete
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
}
