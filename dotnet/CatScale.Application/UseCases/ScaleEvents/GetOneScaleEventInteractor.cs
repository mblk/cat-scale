using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.ScaleEvents;

public interface IGetOneScaleEventInteractor
{
    Task<Response> GetOneScaleEvent(Request request);

    record Request(int Id);

    record Response(ScaleEvent ScaleEvent);

}

public class GetOneScaleEventInteractor : IGetOneScaleEventInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetOneScaleEventInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IGetOneScaleEventInteractor.Response> GetOneScaleEvent(IGetOneScaleEventInteractor.Request request)
    {
        var scaleEvent = await _unitOfWork
            .GetRepository<ScaleEvent>()
            .Query(filter: e => e.Id == request.Id,
                includes: new[]
                {
                    nameof(ScaleEvent.StablePhases),
                    nameof(ScaleEvent.Measurement),
                    nameof(ScaleEvent.Cleaning)
                })
            .SingleOrDefaultAsync();

        if (scaleEvent is null)
            throw new EntityNotFoundException("Scale event not found");

        return new IGetOneScaleEventInteractor.Response(scaleEvent);
    }
}