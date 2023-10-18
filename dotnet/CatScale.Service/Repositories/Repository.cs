using System.Linq.Expressions;
using CatScale.Application.Repository;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Repositories;

public class Repository<T> : IRepository<T>
    where T : class
{
    private readonly DbSet<T> _dbSet;

    public Repository(DbSet<T> dbSet)
    {
        _dbSet = dbSet;
    }

    public async Task<int> Count(Expression<Func<T, bool>>? filter = null)
    {
        var q = _dbSet.AsNoTracking();

        if (filter != null)
            q = q.Where(filter);

        return await q.CountAsync();
    }
    
    public IAsyncEnumerable<T> Query(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? order = null,
        string[]? includes = null,
        int? skip = null,
        int? take = null)
    {
        IQueryable<T> q = _dbSet.AsNoTracking();

        if (filter != null)
            q = q.Where(filter);

        if (order != null)
            q = order(q);

        if (includes != null && includes.Any())
            q = includes.Aggregate(q, (sq, name) => sq.Include(name));

        if (skip != null)
            q = q.Skip(skip.Value);

        if (take != null)
            q = q.Take(take.Value);
        
        return q.AsAsyncEnumerable();
    }

    public void Create(T entity)
    {
        _dbSet.Add(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }
}