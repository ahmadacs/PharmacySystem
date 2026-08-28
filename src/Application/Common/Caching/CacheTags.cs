namespace Application.Common.Caching;

/// <summary>
/// Output-cache tags. Writes that change catalogue or stock evict both so
/// list screens never serve stale inventory after a successful mutation.
/// </summary>
public static class CacheTags
{
    public const string Medicines = "medicines";
    public const string Inventory = "inventory";
}
