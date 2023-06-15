using CatScale.Service.Model.Toilet;
using CatScale.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class GraphController : ControllerBase
{
    private readonly ILogger<GraphController> _logger;
    private readonly IGraphService _graphService;
    
    public GraphController(ILogger<GraphController> logger, IGraphService graphService)
    {
        _logger = logger;
        _graphService = graphService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCatMeasurements(int catId, DateTimeOffset? minTime, DateTimeOffset? maxTime, bool? includeTemperature)
    {
        _logger.LogInformation(
            "GetCatMeasurements catId={catId} minTime={minTime} maxTime={maxTime} includeTemperature={includeTemperature}",
            catId, minTime, maxTime, includeTemperature);
        
        var stream = await _graphService.GetCatMeasurementsGraph(catId, minTime, maxTime, includeTemperature ?? false);
        return File(stream, "image/svg+xml");
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCombinedCatMeasurements(int catId1, int catId2, bool sameAxis, DateTimeOffset? minTime, DateTimeOffset? maxTime)
    {
        var stream = await _graphService.GetCombinedCatMeasurementsGraph(catId1, catId2, sameAxis, minTime, maxTime);
        return File(stream, "image/svg+xml");
    }
    
    [HttpGet]
    public async Task<IActionResult> GetScaleEvent(int scaleEventId)
    {
        var stream = await _graphService.GetScaleEventGraph(scaleEventId);
        return File(stream, "image/svg+xml");
    }
    
    [HttpGet]
    public async Task<IActionResult> GetToiletData(int toiletId, ToiletSensorValue sensorValue)
    {
        var stream = await _graphService.GetToiletGraph(toiletId, sensorValue);
        return File(stream, "image/svg+xml");
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCombinedToiletData(int toiletId, ToiletSensorValue sensorValue1, ToiletSensorValue sensorValue2)
    {
        var stream = await _graphService.GetCombinedToiletGraph(toiletId, sensorValue1, sensorValue2);
        return File(stream, "image/svg+xml");
    }
}