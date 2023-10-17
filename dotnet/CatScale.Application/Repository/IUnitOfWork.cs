namespace CatScale.Application.Repository;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IRepository<T> GetRepository<T>()
        where T: class;
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    
    // TODO add transactions?
}