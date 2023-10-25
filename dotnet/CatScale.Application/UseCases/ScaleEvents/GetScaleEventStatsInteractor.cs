using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.ScaleEvents;

public interface IGetScaleEventStatsInteractor
{
    record Request();

    record Response(Counts AllTime, Counts Yesterday, Counts Today);

    record Counts(int Total, int Cleanings, int Measurements);

    Task<Response> GetScaleEventStats(Request request);
}

public class GetScaleEventStatsInteractor : IGetScaleEventStatsInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetScaleEventStatsInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IGetScaleEventStatsInteractor.Response> GetScaleEventStats(
        IGetScaleEventStatsInteractor.Request request)
    {
        // TODO use client timezone
        var tzi = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        var today = new DateTimeOffset(DateTime.UtcNow.Date);

        today = tzi.IsDaylightSavingTime(today)
            ? today.Add(-tzi.BaseUtcOffset).AddHours(-1)
            : today.Add(-tzi.BaseUtcOffset);

        var yesterday = today.AddDays(-1);

        var scaleEventsRepo = _unitOfWork.GetRepository<ScaleEvent>();
        var cleaningsRepo = _unitOfWork.GetRepository<Cleaning>();
        var measurementsRepo = _unitOfWork.GetRepository<Measurement>();

        var numScaleEventsToday = await scaleEventsRepo
            .Count(e => e.StartTime >= today);
        var numScaleEventsYesterday = await scaleEventsRepo
            .Count(e => e.StartTime >= yesterday && e.StartTime < today);
        var numScaleEvents = await scaleEventsRepo
            .Count();

        var numCleaningsToday = await cleaningsRepo
            .Count(e => e.Timestamp >= today);
        var numCleaningsYesterday = await cleaningsRepo
            .Count(e => e.Timestamp >= yesterday && e.Timestamp < today);
        var numCleanings = await cleaningsRepo
            .Count();

        var numMeasurementsToday = await measurementsRepo
            .Count(e => e.Timestamp >= today);
        var numMeasurementsYesterday = await measurementsRepo
            .Count(e => e.Timestamp >= yesterday && e.Timestamp < today);
        var numMeasurements = await measurementsRepo
            .Count();

        return new IGetScaleEventStatsInteractor.Response(
            AllTime: new IGetScaleEventStatsInteractor.Counts(
                Total: numScaleEvents,
                Cleanings: numCleanings,
                Measurements: numMeasurements),
            Yesterday: new IGetScaleEventStatsInteractor.Counts(
                Total: numScaleEventsYesterday,
                Cleanings: numCleaningsYesterday,
                Measurements: numMeasurementsYesterday),
            Today: new IGetScaleEventStatsInteractor.Counts(
                Total: numScaleEventsToday,
                Cleanings: numCleaningsToday,
                Measurements: numMeasurementsToday)
        );
    }
}