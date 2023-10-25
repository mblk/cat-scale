using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Application.Services;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.ScaleEvents;

public interface IClassifyScaleEventInteractor
{
    record Request(int Id);

    record Response(ScaleEvent ScaleEvent);

    Task<Response> ClassifyScaleEvent(Request request);
}

public class ClassifyScaleEventInteractor : IClassifyScaleEventInteractor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClassificationService _classificationService;

    public ClassifyScaleEventInteractor(IUnitOfWork unitOfWork,
        IClassificationService classificationService
        )
    {
        _unitOfWork = unitOfWork;
        _classificationService = classificationService;
    }

    public async Task<IClassifyScaleEventInteractor.Response> ClassifyScaleEvent(
        IClassifyScaleEventInteractor.Request request)
    {
        var repo = _unitOfWork.GetRepository<ScaleEvent>();

        var scaleEvent = await repo
            .Query(filter: e => e.Id == request.Id,
                includes: new[]
                {
                    nameof(ScaleEvent.StablePhases),
                    nameof(ScaleEvent.Cleaning),
                    nameof(ScaleEvent.Measurement)
                })
            .SingleOrDefaultAsync();

        if (scaleEvent is null)
            throw new EntityNotFoundException("Scale event not found");
        
        var cats = await _unitOfWork.GetRepository<Cat>()
            .Query(includes: new[] { nameof(Cat.Weights) })
            .ToArrayAsync();

        _classificationService.ClassifyScaleEvent(cats, scaleEvent);
        
        repo.Update(scaleEvent);

        await _unitOfWork.SaveChangesAsync();

        return new IClassifyScaleEventInteractor.Response(scaleEvent);
    }
}