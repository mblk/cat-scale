using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Toilets;

public interface IGetAllToiletsInteractor
{
    record Request();

    record Response(IAsyncEnumerable<Toilet> Toilets);

    Task<Response> GetAllToilets(Request request);
}

public class GetAllToiletsInteractor : IGetAllToiletsInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllToiletsInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public Task<IGetAllToiletsInteractor.Response> GetAllToilets(IGetAllToiletsInteractor.Request request)
    {
        var toilets = _unitOfWork
            .GetRepository<Toilet>()
            .Query(
                order: x => x.OrderBy(t => t.Id)
            );

        return Task.FromResult(new IGetAllToiletsInteractor.Response(toilets));
    }
}