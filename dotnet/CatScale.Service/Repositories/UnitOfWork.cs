using CatScale.Service.DbModel;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Repositories;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IRepository<T> GetRepository<T>() where T: class;
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    
    // TODO add transactions
}

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly CatScaleDbContext _dbContext;

    public UnitOfWork(CatScaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IRepository<T> GetRepository<T>() where T : class
    {
        var set = _dbContext.Set<T>();
        return new Repository<T>(set);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException e) // TODO figure out what to do in this case ^^
        {
            Console.WriteLine($"SaveChangesAsync: DbUpdateConcurrencyException: {e}");
            throw;
        }
        catch (Exception e)
        {
            Console.WriteLine($"SaveChangesAsync: {e}");
            throw;
        }
    }
    
    public void Dispose()
    {
        _dbContext.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }
}