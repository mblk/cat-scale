using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Toilets;

public interface IGetOneToiletInteractor
{
    record Request(int Id);

    record Response(Toilet Toilet);

    Task<Response> GetOneToilet(Request request);
}

public class GetOneToiletInteractor : IGetOneToiletInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOneToiletInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IGetOneToiletInteractor.Response> GetOneToilet(IGetOneToiletInteractor.Request request)
    {
        var toilet = await _unitOfWork
            .GetRepository<Toilet>()
            .Query(filter: t => t.Id == request.Id)
            .SingleOrDefaultAsync();

        if (toilet is null)
            throw new EntityNotFoundException("Toilet not found");

        return new IGetOneToiletInteractor.Response(toilet);
    }
}