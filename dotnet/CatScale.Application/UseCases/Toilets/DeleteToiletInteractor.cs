using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Toilets;

public interface IDeleteToiletInteractor
{
    record Request(int Id);
    
    record Response();
    
    Task<Response> DeleteToilet(Request request);
}

public class DeleteToiletInteractor : IDeleteToiletInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteToiletInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<IDeleteToiletInteractor.Response> DeleteToilet(IDeleteToiletInteractor.Request request)
    {
        var toiletRepo = _unitOfWork.GetRepository<Toilet>();
        var scaleEventRepo = _unitOfWork.GetRepository<ScaleEvent>();

        var toilet = await toiletRepo
            .Query(filter: t => t.Id == request.Id)
            .SingleOrDefaultAsync();
        if (toilet is null)
            throw new EntityNotFoundException("Toilet not found");

        var numScaleEvents = await scaleEventRepo.Count(e => e.ToiletId == request.Id);
        if (numScaleEvents > 10)
            throw new DomainValidationException("Toilet has too many scale events to be deleted");
        
        toiletRepo.Delete(toilet);
        await _unitOfWork.SaveChangesAsync();

        return new IDeleteToiletInteractor.Response();
    }
}