using Application.Common.Interfaces;
using Microsoft.AspNetCore.OutputCaching;

namespace WebApi.Caching;

public sealed class OutputCacheInvalidator : ICacheInvalidator
{
    private readonly IOutputCacheStore _store;

    public OutputCacheInvalidator(IOutputCacheStore store)
    {
        _store = store;
    }

    public async Task EvictByTagsAsync(IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        foreach (var tag in tags.Distinct(StringComparer.Ordinal))
            await _store.EvictByTagAsync(tag, cancellationToken);
    }
}
