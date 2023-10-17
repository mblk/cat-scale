using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Cats;

public class GetAllCatsInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllCatsInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IAsyncEnumerable<Cat> GetAllCats()
    {
        return _unitOfWork.GetRepository<Cat>()
            .Query(order: x => x.OrderBy(c => c.Id));
    }
}