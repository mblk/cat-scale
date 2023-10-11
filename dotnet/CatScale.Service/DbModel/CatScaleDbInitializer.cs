using CatScale.Domain.Model;

namespace CatScale.Service.DbModel;

public static class CatScaleDbInitializer
{
    public static void Initialize(CatScaleDbContext dbContext)
    {
        if (dbContext.Toilets.Any())
            return;

        var toilets = new Toilet[]
        {
            new Toilet()
            {
                Name = "Katzenklo 1",
                Description = "Das weiße Katzenklo im Badezimmer.",
            }
        };

        var cats = new Cat[]
        {
            new Cat()
            {
                Name = "Filou",
                DateOfBirth = new DateOnly(2014, 7, 7),
                Weights = new List<CatWeight>()
                {
                    new CatWeight() { Timestamp = DateTimeOffset.Now.ToUniversalTime(), Weight = 7200d },
                }
            },
            new Cat()
            {
                Name = "Felix",
                DateOfBirth = new DateOnly(2017, 7, 1),
                Weights = new List<CatWeight>()
                {
                    new CatWeight() {Timestamp = DateTimeOffset.Now.ToUniversalTime(), Weight = 6000d },
                }
            },
        };

        dbContext.Toilets.AddRange(toilets);
        dbContext.Cats.AddRange(cats);
        
        dbContext.SaveChanges();
    }
}