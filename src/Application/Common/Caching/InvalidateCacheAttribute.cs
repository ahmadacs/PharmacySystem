namespace Application.Common.Caching;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InvalidateCacheAttribute : Attribute
{
    public InvalidateCacheAttribute(params string[] tags)
    {
        Tags = tags;
    }

    public string[] Tags { get; }
}
