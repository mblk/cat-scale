using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Application.Services;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.ScaleEvents;

public interface IDeleteScaleEventInteractor
{
    record Request(int ScaleEventId);

    record Response();

    Task<Response> DeleteScaleEvent(Request request);
}

public class DeleteScaleEventInteractor : IDeleteScaleEventInteractor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public DeleteScaleEventInteractor(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }
    
    public async Task<IDeleteScaleEventInteractor.Response> DeleteScaleEvent(IDeleteScaleEventInteractor.Request request)
    {
        var repo = _unitOfWork.GetRepository<ScaleEvent>();

        var scaleEvent = await repo
            .Query(filter: e => e.Id == request.ScaleEventId)
            .SingleOrDefaultAsync();

        if (scaleEvent is null)
            throw new EntityNotFoundException("Scale event not found");
        
        repo.Delete(scaleEvent);

        await _unitOfWork.SaveChangesAsync();
 
        _notificationService.NotifyScaleEventsChanged();

        return new IDeleteScaleEventInteractor.Response();
    }
}