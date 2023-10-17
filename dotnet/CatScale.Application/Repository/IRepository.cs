using System.Linq.Expressions;

namespace CatScale.Application.Repository;

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