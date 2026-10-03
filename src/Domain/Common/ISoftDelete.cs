namespace Domain.Common
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        Guid? DeletedBy { get; set; }
        DateTime? DeletedAt { get; set; }
    }
}