using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Application.Services;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.ScaleEvents;

public interface ICreateScaleEventInteractor
{
    record Request(int ToiletId, DateTimeOffset StartTime, DateTimeOffset EndTime,
        (DateTimeOffset Timestamp, double Length, double Value)[] StablePhases,
        double Temperature, double Humidity, double Pressure
    );

    record Response(ScaleEvent ScaleEvent);

    Task<Response> CreateScaleEvent(Request request);
}

public class CreateScaleEventInteractor : ICreateScaleEventInteractor
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClassificationService _classificationService;
    private readonly INotificationService _notificationService;

    public CreateScaleEventInteractor(IUnitOfWork unitOfWork,
        IClassificationService classificationService,
        INotificationService notificationService
    )
    {
        _unitOfWork = unitOfWork;
        _classificationService = classificationService;
        _notificationService = notificationService;
    }

    public async Task<ICreateScaleEventInteractor.Response> CreateScaleEvent(
        ICreateScaleEventInteractor.Request request)
    {
        await ValidateRequest(request);

        var scaleEvent = CreateScaleEventFromRequest(request);

        var cats = await _unitOfWork
            .GetRepository<Cat>()
            .Query(includes: new [] { nameof(Cat.Weights) })
            .ToArrayAsync();

        _classificationService.ClassifyScaleEvent(cats, scaleEvent);

        _unitOfWork.GetRepository<ScaleEvent>()
            .Create(scaleEvent);

        await _unitOfWork.SaveChangesAsync();

        _notificationService.NotifyScaleEventsChanged();

        return new ICreateScaleEventInteractor.Response(scaleEvent);
    }

    private async Task ValidateRequest(ICreateScaleEventInteractor.Request request)
    {
        CheckIfDatesArePlausible(request);
        CheckIfStablePhasesAreOutOfBounds(request);
        await CheckIfToiletExists(request);
        await CheckIfEventAlreadyExists(request);
    }

    private static void CheckIfDatesArePlausible(ICreateScaleEventInteractor.Request request)
    {
        if (request.EndTime < request.StartTime.AddSeconds(5))
            throw new DomainValidationException("Event too short");

        if (request.EndTime > request.StartTime.AddMinutes(15))
            throw new DomainValidationException("Event too long");

        if (request.StartTime > DateTimeOffset.Now)
            throw new DomainValidationException("Start time is in future");

        if (request.StartTime < DateTimeOffset.Now.AddDays(-7))
            throw new DomainValidationException("Start time is too far in the past");
    }

    private static void CheckIfStablePhasesAreOutOfBounds(ICreateScaleEventInteractor.Request request)
    {
        if (request.StablePhases.Any(e =>
                e.Timestamp.AddSeconds(-e.Length) < request.StartTime || e.Timestamp > request.EndTime))
            throw new DomainValidationException("Stable phase outside of event bounds");
    }

    private async Task CheckIfToiletExists(ICreateScaleEventInteractor.Request request)
    {
        var toiletExists = await _unitOfWork.GetRepository<Toilet>()
            .Query(filter: t => t.Id == request.ToiletId)
            .AnyAsync();

        if (!toiletExists)
            throw new EntityNotFoundException("Toilet not found");
    }

    private async Task CheckIfEventAlreadyExists(ICreateScaleEventInteractor.Request request)
    {
        var repo = _unitOfWork.GetRepository<ScaleEvent>();

        var justBeforeStart = request.StartTime.ToUniversalTime().AddSeconds(-1);
        var justAfterStart = request.StartTime.ToUniversalTime().AddSeconds(1);

        var eventAlreadyExists = await repo
            .Query(filter: e =>
                justBeforeStart < e.StartTime && e.StartTime < justAfterStart &&
                e.ToiletId == request.ToiletId)
            .AnyAsync();

        if (eventAlreadyExists)
            throw new DomainValidationException("A scale event with the same start time already exists");
    }

    private static ScaleEvent CreateScaleEventFromRequest(ICreateScaleEventInteractor.Request request)
    {
        return new ScaleEvent
        {
            ToiletId = request.ToiletId,
            StartTime = request.StartTime.ToUniversalTime(),
            EndTime = request.EndTime.ToUniversalTime(),
            StablePhases = request.StablePhases.Select(x => new StablePhase()
            {
                Timestamp = x.Timestamp.ToUniversalTime(),
                Length = x.Length,
                Value = x.Value
            }).ToList(),
            Temperature = request.Temperature,
            Humidity = request.Humidity,
            Pressure = request.Pressure,
        };
    }
}