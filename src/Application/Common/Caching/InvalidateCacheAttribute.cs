namespace Application.Common.Caching;

/// <summary>
/// After a command succeeds, evict output-cache entries that use these tags.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InvalidateCacheAttribute : Attribute
{
    public InvalidateCacheAttribute(params string[] tags)
    {
        Tags = tags;
    }

    public string[] Tags { get; }
}
