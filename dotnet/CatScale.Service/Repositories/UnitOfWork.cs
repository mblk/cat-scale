using CatScale.Service.DbModel;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Repositories;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IFoodRepository FoodRepository { get; }
    IFeedingRepository FeedingRepository { get; }

    IRepository<T> GetRepository<T>() where T: class;
    
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly CatScaleContext _dbContext;
    
    public IFoodRepository FoodRepository { get; }
    public IFeedingRepository FeedingRepository { get; }

    public UnitOfWork(CatScaleContext dbContext)
    {
        Console.WriteLine("UnitOfWork ctor");
        
        _dbContext = dbContext;

        FoodRepository = new FoodRepository(dbContext);
        FeedingRepository = new FeedingRepository(dbContext);
    }

    public IRepository<T> GetRepository<T>() where T : class
    {
        var set = _dbContext.Set<T>();
        return new Repository<T>(set);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("UnitOfWork.SaveChangesAsync");

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
        Console.WriteLine("UnitOfWork.Dispose");
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("UnitOfWork.DisposeAsync");
        await _dbContext.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}