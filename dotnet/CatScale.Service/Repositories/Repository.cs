using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Repositories;

public interface IRepository<T>
    where T: class
{
    IQueryable<T> Get(Expression<Func<T, bool>>? condition = null);
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
    
    public IQueryable<T> Get(Expression<Func<T, bool>>? condition = null)
    {
        return condition is null
            ? _dbSet.AsNoTracking()
            : _dbSet.AsNoTracking().Where(condition);
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