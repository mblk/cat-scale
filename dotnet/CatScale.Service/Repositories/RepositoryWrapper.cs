using CatScale.Service.DbModel;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Repositories;

public interface IRepositoryWrapper
{
    IFoodRepository FoodRepository { get; }
    IFeedingRepository FeedingRepository { get; }

    Task Save();
}

public class RepositoryWrapper : IRepositoryWrapper
{
    private readonly CatScaleContext _dbContext;
    
    public IFoodRepository FoodRepository { get; }
    public IFeedingRepository FeedingRepository { get; }

    public RepositoryWrapper(CatScaleContext dbContext)
    {
        Console.WriteLine("RepositoryWrapper ctor");
        
        _dbContext = dbContext;

        FoodRepository = new FoodRepository(dbContext);
        FeedingRepository = new FeedingRepository(dbContext);
    }
    
    public async Task Save()
    {
        Console.WriteLine("RepositoryWrapper.Save");

        try
        {
            await _dbContext.SaveChangesAsync();
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
}