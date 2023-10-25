using CatScale.Application.Services;
using CatScale.Application.UseCases.ScaleEvents;
using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.ScaleEvent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ScaleEventController : ControllerBase
{
    private readonly ILogger<ScaleEventController> _logger;
    private readonly CatScaleDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public ScaleEventController(ILogger<ScaleEventController> logger, CatScaleDbContext dbContext,
        INotificationService notificationService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    [HttpGet]
    [HttpGet("{toiletId:int?}")]
    public async Task<ActionResult<IAsyncEnumerable<ScaleEventDto>>> GetAll(
        [FromServices] IGetAllScaleEventsInteractor interactor,
        [FromRoute] int? toiletId,
        [FromQuery] int? skip,
        [FromQuery] int? take)
    {
        _logger.LogInformation("GetAll {ToiledId} {Skip} {Take}", toiletId, skip, take);

        var response = await interactor
            .GetAllScaleEvents(new IGetAllScaleEventsInteractor.Request(toiletId, skip, take));

        var scaleEvents = response.ScaleEvents
            .Select(DataMapper.MapScaleEvent);

        Response.Headers.Add("X-Total-Count", response.Count.ToString());

        return Ok(scaleEvents);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ScaleEventDto>> GetOne(
        [FromServices] IGetOneScaleEventInteractor interactor,
        [FromRoute] int id)
    {
        var response = await interactor
            .GetOneScaleEvent(new IGetOneScaleEventInteractor.Request(id));

        return Ok(DataMapper.MapScaleEvent(response.ScaleEvent));
    }

    // TODO does not work if 'Roles' is set ??
    //[Authorize(AuthenticationSchemes = "ApiKey, Identity.Application", Roles = ApplicationRoles.Admin)]
    [Authorize(AuthenticationSchemes = "ApiKey, Identity.Application")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromServices] ICreateScaleEventInteractor interactor,
        [FromBody] NewScaleEvent newScaleEvent)
    {
        _logger.LogInformation("Creating scale event: {NewScaleEvent}", newScaleEvent);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        int toiletId = newScaleEvent.ToiletId!.Value;
        DateTimeOffset startTime = newScaleEvent.StartTime!.Value;
        DateTimeOffset endTime = newScaleEvent.EndTime!.Value;
        double temperature = newScaleEvent.Temperature!.Value;
        double humidity = newScaleEvent.Humidity!.Value;
        double pressure = newScaleEvent.Pressure!.Value;

        (DateTimeOffset, double, double)[] stablePhases = newScaleEvent.StablePhases!
            .Select(sp => (sp.Timestamp!.Value, sp.Length!.Value, sp.Value!.Value))
            .ToArray();

        var response = await interactor.CreateScaleEvent(
            new ICreateScaleEventInteractor.Request(toiletId, startTime, endTime,
                stablePhases, temperature, humidity, pressure));

        return CreatedAtAction(nameof(GetOne),
            new { id = response.ScaleEvent.Id },
            DataMapper.MapScaleEvent(response.ScaleEvent));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation($"Deleting scale event {id}");

        var scaleEvent = await _dbContext.ScaleEvents
            .SingleOrDefaultAsync(x => x.Id == id);

        if (scaleEvent is null)
            return NotFound();

        _dbContext.ScaleEvents.Remove(scaleEvent);
        await _dbContext.SaveChangesAsync();

        _notificationService.NotifyScaleEventsChanged();

        return Ok();
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost("{id:int}")]
    public async Task<IActionResult> Classify(int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation($"Classifying scale event {id}");

        var cats = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Weights)
            .ToArrayAsync();

        var scaleEvent = await _dbContext.ScaleEvents
            .Include(x => x.StablePhases)
            .Include(x => x.Cleaning)
            .Include(x => x.Measurement)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (scaleEvent is null)
            return NotFound();

        //_classificationService.ClassifyScaleEvent(cats, scaleEvent);
        await _dbContext.SaveChangesAsync();

        _notificationService.NotifyScaleEventsChanged();

        return Ok();
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost]
    public async Task<IActionResult> ClassifyAllEvents()
    {
        _logger.LogInformation($"Classifying all scale events");

        var cats = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Weights)
            .ToArrayAsync();

        var scaleEvents = _dbContext.ScaleEvents
            .Include(x => x.StablePhases)
            .Include(x => x.Cleaning)
            .Include(x => x.Measurement);

        foreach (var scaleEvent in scaleEvents)
        {
            //_classificationService.ClassifyScaleEvent(cats, scaleEvent);
        }

        await _dbContext.SaveChangesAsync();

        _notificationService.NotifyScaleEventsChanged();

        return Ok();
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost]
    public async Task<IActionResult> DeleteAllClassifications()
    {
        _logger.LogInformation($"Deleting all classifications");

        _dbContext.Cleanings.RemoveRange(_dbContext.Cleanings);
        _dbContext.Measurements.RemoveRange(_dbContext.Measurements);

        await _dbContext.SaveChangesAsync();

        _notificationService.NotifyScaleEventsChanged();

        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<ScaleEventStats>> GetStats()
    {
        // TODO use client timezone
        var tzi = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        var today = new DateTimeOffset(DateTime.UtcNow.Date);

        today = tzi.IsDaylightSavingTime(today)
            ? today.Add(-tzi.BaseUtcOffset).AddHours(-1)
            : today.Add(-tzi.BaseUtcOffset);

        var yesterday = today.AddDays(-1);

        var numScaleEventsToday = await _dbContext.ScaleEvents
            .Where(e => e.StartTime >= today)
            .CountAsync();
        var numScaleEventsYesterday = await _dbContext.ScaleEvents
            .Where(e => e.StartTime >= yesterday && e.StartTime < today)
            .CountAsync();
        var numScaleEvents = await _dbContext.ScaleEvents.CountAsync();

        var numCleaningToday = await _dbContext.Cleanings
            .Where(c => c.Timestamp >= today)
            .CountAsync();
        var numCleaningYesterday = await _dbContext.Cleanings
            .Where(c => c.Timestamp >= yesterday && c.Timestamp < today)
            .CountAsync();
        var numCleaning = await _dbContext.Cleanings.CountAsync();

        var numMeasurementsToday = await _dbContext.Measurements
            .Where(m => m.Timestamp >= today)
            .CountAsync();
        var numMeasurementsYesterday = await _dbContext.Measurements
            .Where(m => m.Timestamp >= yesterday && m.Timestamp < today)
            .CountAsync();
        var numMeasurements = await _dbContext.Measurements.CountAsync();

        // Ideas:
        // - poos since last cleaning?
        // - avg. events/measurements/cleanings per day?
        // ... ?

        return Ok(new ScaleEventStats(
            new ScaleEventCounts(numScaleEvents, numCleaning, numMeasurements),
            new ScaleEventCounts(numScaleEventsYesterday, numCleaningYesterday, numMeasurementsYesterday),
            new ScaleEventCounts(numScaleEventsToday, numCleaningToday, numMeasurementsToday)));
    }

    [HttpGet]
    public ActionResult<PooCount[]> GetPooCounts()
    {
        var activeCats = _dbContext.Cats
            .Where(c => c.Type == CatType.Active)
            .Select(c => c.Id)
            .ToArray();

        var result = new List<PooCount>();

        var toilets = _dbContext.Toilets
            .OrderBy(t => t.Id)
            .Include(t => t.ScaleEvents)
            .ThenInclude(e => e.Cleaning)
            .Include(t => t.ScaleEvents)
            .ThenInclude(e => e.Measurement);

        foreach (var toilet in toilets)
        {
            var lastCleaningTime = toilet
                                       .ScaleEvents
                                       .Where(e => e.Cleaning != null)
                                       .MaxBy(e => e.StartTime)
                                       ?.StartTime
                                   ?? DateTimeOffset.MinValue;

            var numMeasurementsSinceLastCleaning = toilet.ScaleEvents
                .Count(e =>
                    e.StartTime > lastCleaningTime &&
                    e.Measurement != null &&
                    activeCats.Contains(e.Measurement.CatId // TODO check how this translates to sql
                    ));

            result.Add(new PooCount(toilet.Id, numMeasurementsSinceLastCleaning));
        }

        return Ok(result.ToArray());
    }

    [HttpGet]
    public async Task Subscribe(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Streaming events ...");

        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("X-Accel-Buffering", "no");

        _notificationService.ScaleEventsChanged += handler;

        async void handler()
        {
            try
            {
                _logger.LogInformation($"Sending change event ...");

                await Response.WriteAsync($"data: ScaleEventsChanged\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Cancelled");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Failed to send event");
            }
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Cancelled");
        }
        finally
        {
            _notificationService.ScaleEventsChanged -= handler;
        }
    }
}