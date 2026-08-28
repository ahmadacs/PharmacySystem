using Domain.Common;

namespace Domain.Entities.Medicines;

public class GenericName : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }

    private readonly List<Medicine> _medicines = new();
    public IReadOnlyCollection<Medicine> Medicines => _medicines.AsReadOnly();

    private GenericName() { }

    public GenericName(string name, string? nameAr = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scientific name is required.", nameof(name));

        Name = name.Trim();
        NameAr = nameAr?.Trim();
    }

    public void Rename(string name, string? nameAr = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scientific name is required.", nameof(name));

        Name = name.Trim();
        NameAr = nameAr?.Trim();
    }
}