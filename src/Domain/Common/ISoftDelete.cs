namespace Domain.Common
{
    public interface ISoftDelete : IEntity
    {
        bool IsDeleted { get; set; }
        Guid? DeletedBy { get; set; }
        DateTime? DeletedAt { get; set; }
    }
}