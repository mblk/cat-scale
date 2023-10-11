using CatScale.Domain.Model;
using CatScale.Service.DbModel;

namespace CatScale.Service.Repositories;

public interface IFoodRepository : IRepository<Food>
{
}

public class FoodRepository : Repository<Food>, IFoodRepository
{
    public FoodRepository(CatScaleDbContext dbContext) : base(dbContext.Foods)
    {
    }
}

public interface IFeedingRepository : IRepository<Feeding>
{
}

public class FeedingRepository : Repository<Feeding>, IFeedingRepository
{
    public FeedingRepository(CatScaleDbContext dbContext) : base(dbContext.Feedings)
    {
    }
}