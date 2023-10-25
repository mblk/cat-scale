using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.ScaleEvents;

public interface IGetPooCountInteractor
{
    record Request();

    record Response(Count[] Counts);

    record Count(int ToiletId, int PooCount);

    Task<Response> GetPooCount(Request request);
}

public class GetPooCountInteractor : IGetPooCountInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPooCountInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IGetPooCountInteractor.Response> GetPooCount(IGetPooCountInteractor.Request request)
    {
        var catRepo = _unitOfWork.GetRepository<Cat>();
        var toiletRepo = _unitOfWork.GetRepository<Toilet>();
        //var cleaningRepo = _unitOfWork.GetRepository<Cleaning>();
        //var measurementRepo = _unitOfWork.GetRepository<Measurement>();
        var scaleEventRepo = _unitOfWork.GetRepository<ScaleEvent>();

        // var activeCatIds = await catRepo
        //     .Query(filter: c => c.Type == CatType.Active)
        //     .Select(c => c.Id)
        //     .ToArrayAsync();

        var result = new List<IGetPooCountInteractor.Count>();

        var toilets = await toiletRepo
            .Query(order: x => x.OrderBy(t => t.Id))
            .ToArrayAsync();

        foreach (var toilet in toilets)
        {
            var lastCleaning = await scaleEventRepo
                .Query(
                    filter: e => e.ToiletId == toilet.Id && e.Cleaning != null,
                    order: x => x.OrderByDescending(c => c.StartTime))
                .FirstOrDefaultAsync();

            var lastCleaningTime = lastCleaning?.StartTime ?? DateTimeOffset.MinValue;

            var numMeasurementsSinceLastCleaning = await scaleEventRepo
                .Count(e => e.ToiletId == toilet.Id &&
                            e.Measurement != null &&
                            e.StartTime > lastCleaningTime);
            
            result.Add(new IGetPooCountInteractor.Count(toilet.Id, numMeasurementsSinceLastCleaning));
        }

        // var toilets = _dbContext.Toilets
        //     .OrderBy(t => t.Id)
        //     .Include(t => t.ScaleEvents)
        //     .ThenInclude(e => e.Cleaning)
        //     .Include(t => t.ScaleEvents)
        //     .ThenInclude(e => e.Measurement);
        //
        // foreach (var toilet in toilets)
        // {
        //     var lastCleaningTime = toilet
        //                                .ScaleEvents
        //                                .Where(e => e.Cleaning != null)
        //                                .MaxBy(e => e.StartTime)
        //                                ?.StartTime
        //                            ?? DateTimeOffset.MinValue;
        //
        //     var numMeasurementsSinceLastCleaning = toilet.ScaleEvents
        //         .Count(e =>
        //             e.StartTime > lastCleaningTime &&
        //             e.Measurement != null &&
        //             activeCats.Contains(e.Measurement.CatId // TODO check how this translates to sql
        //             ));
        //
        //     result.Add(new PooCount(toilet.Id, numMeasurementsSinceLastCleaning));
        // }

        return new IGetPooCountInteractor.Response(result.ToArray());
    }
}