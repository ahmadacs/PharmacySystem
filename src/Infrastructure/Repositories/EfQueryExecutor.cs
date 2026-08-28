using Application.Common.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class EfQueryExecutor : IAsyncQueryExecutor
{
    private readonly ApplicationDbContext _db;

    public EfQueryExecutor(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<int> CountAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default)
        => source.CountAsync(cancellationToken);

    public Task<bool> AnyAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default)
        => source.AnyAsync(cancellationToken);

    public Task<List<T>> ToListAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default)
        => source.ToListAsync(cancellationToken);

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default)
        => source.FirstOrDefaultAsync(cancellationToken);

    public Task<T?> SingleOrDefaultAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default)
        => source.SingleOrDefaultAsync(cancellationToken);
}