using Domain.Common;

namespace Infrastructure.Identity;

public class Permission : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Group { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Permission() { }

    public Permission(string name, string group, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentException("Permission group is required.", nameof(group));

        Name = name.Trim();
        Group = group.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void Rename(string name, string group)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(group))
            throw new ArgumentException("Permission group is required.", nameof(group));

        Name = name.Trim();
        Group = group.Trim();
    }
}
