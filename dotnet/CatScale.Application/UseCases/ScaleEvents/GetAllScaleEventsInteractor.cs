using System.Linq.Expressions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.ScaleEvents;

public interface IGetAllScaleEventsInteractor
{
    Task<Response> GetAllScaleEvents(Request request);

    record Request(int? ToiletId, int? Skip, int? Take);

    record Response(int Count, IAsyncEnumerable<ScaleEvent> ScaleEvents);
}

public class GetAllScaleEventsInteractor : IGetAllScaleEventsInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllScaleEventsInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IGetAllScaleEventsInteractor.Response> GetAllScaleEvents(
        IGetAllScaleEventsInteractor.Request request)
    {
        var repo = _unitOfWork.GetRepository<ScaleEvent>();

        Expression<Func<ScaleEvent, bool>>? filter = request.ToiletId != null
            ? t => t.ToiletId == request.ToiletId
            : null;

        var count = await repo.Count(filter);

        var scaleEvents = repo
            .Query(
                filter: filter,
                order: x => x.OrderByDescending(e => e.EndTime),
                includes: new[]
                {
                    nameof(ScaleEvent.StablePhases),
                    nameof(ScaleEvent.Measurement),
                    nameof(ScaleEvent.Cleaning)
                },
                skip: request.Skip,
                take: request.Take);

        return new IGetAllScaleEventsInteractor.Response(count, scaleEvents);
    }
}