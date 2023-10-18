using CatScale.Application.Repository;
using CatScale.Application.UseCases.Cats;
using CatScale.Domain.Model;
using Moq;

namespace CatScale.Application.Tests.UseCases.Cats;

public class GetAllCatsInteractorTests
{
    [Fact]
    public async Task GetAllCats_Should_ReturnEmptyList_When_NoCatsExist()
    {
        var catRepoMock = new Mock<IRepository<Cat>>(MockBehavior.Strict);
        catRepoMock.Setup(m => m.Query(
            null, 
            It.IsAny<Func<IQueryable<Cat>, IOrderedQueryable<Cat>>>(), 
            null,
            null,
            null))
            .Returns(Array.Empty<Cat>().ToAsyncEnumerable());
        var catRepo = catRepoMock.Object;
        
        var uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
        uowMock.Setup(m => m.GetRepository<Cat>()).Returns(catRepo);
        var unitOfWork = uowMock.Object;

        var cats = await new GetAllCatsInteractor(unitOfWork)
            .GetAllCats()
            .ToListAsync();

        Assert.Empty(cats);
    }
}