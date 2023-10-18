using System.Linq.Expressions;

namespace CatScale.Application.Repository;

public interface IRepository<T>
    where T: class
{
    Task<int> Count(Expression<Func<T, bool>>? filter = null);
    
    IAsyncEnumerable<T> Query(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? order = null,
        string[]? includes = null,
        int? skip = null,
        int? take = null
    );

    void Create(T entity);
    void Update(T entity);
    void Delete(T entity);
}