namespace Application.Common.Interfaces;

public interface ICacheInvalidator
{
    Task EvictByTagsAsync(IReadOnlyList<string> tags, CancellationToken cancellationToken = default);
}
