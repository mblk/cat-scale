using CatScale.Application.Services;
using CatScale.Application.UseCases.ScaleEvents;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.ScaleEvent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ScaleEventController : ControllerBase
{
    private readonly ILogger<ScaleEventController> _logger;
    private readonly INotificationService _notificationService;

    public ScaleEventController(ILogger<ScaleEventController> logger, 
        INotificationService notificationService)
    {
        _logger = logger;
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
        [FromServices] IDeleteScaleEventInteractor interactor,
        [FromRoute] int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _ = await interactor.DeleteScaleEvent(
            new IDeleteScaleEventInteractor.Request(
                id));

        return Ok();
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost("{id:int}")]
    public async Task<IActionResult> Classify(
        [FromServices] IClassifyScaleEventInteractor interactor,
        [FromRoute] int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _ = await interactor.ClassifyScaleEvent(
            new IClassifyScaleEventInteractor.Request(
                id));

        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<ScaleEventStats>> GetStats(
        [FromServices] IGetScaleEventStatsInteractor interactor)
    {
        // TODO use client timezone ?
        
        var response = await interactor.GetScaleEventStats(
            new IGetScaleEventStatsInteractor.Request());

        return Ok(new ScaleEventStats(
            AllTime: new ScaleEventCounts(
                Total: response.AllTime.Total,
                Cleanings: response.AllTime.Cleanings,
                Measurements: response.AllTime.Measurements),
            Yesterday: new ScaleEventCounts(
                Total: response.Yesterday.Total,
                Cleanings: response.Yesterday.Cleanings,
                Measurements: response.Yesterday.Measurements),
            Today: new ScaleEventCounts(
                Total: response.Today.Total,
                Cleanings: response.Today.Cleanings,
                Measurements: response.Today.Measurements)));
    }

    [HttpGet]
    public async Task<ActionResult<PooCount[]>> GetPooCounts(
        [FromServices] IGetPooCountInteractor interactor)
    {
        var response = await interactor.GetPooCount(new IGetPooCountInteractor.Request());

        var pooCounts = response.Counts
            .Select(c => new PooCount(c.ToiletId, c.PooCount))
            .ToArray();

        return Ok(pooCounts);
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