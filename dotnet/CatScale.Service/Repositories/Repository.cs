using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Repositories;

public interface IRepository<T>
    where T: class
{
    IAsyncEnumerable<T> Query(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? order = null,
        params string[] includes
    );

    void Create(T entity);
    void Update(T entity);
    void Delete(T entity);
}

public class Repository<T> : IRepository<T>
    where T : class
{
    private readonly DbSet<T> _dbSet;

    public Repository(DbSet<T> dbSet)
    {
        _dbSet = dbSet;
    }
    
    public IAsyncEnumerable<T> Query(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? order = null,
        params string[] includes
        )
    {
        IQueryable<T> q = _dbSet.AsNoTracking();

        if (filter != null)
            q = q.Where(filter);

        if (order != null)
            q = order(q);

        if (includes.Any())
            q = includes.Aggregate(q, (sq, name) => sq.Include(name));

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