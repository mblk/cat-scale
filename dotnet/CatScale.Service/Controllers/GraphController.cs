using CatScale.Service.Model.Toilet;
using CatScale.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class GraphController : ControllerBase
{
    private readonly IGraphService _graphService;
    
    public GraphController(IGraphService graphService)
    {
        _graphService = graphService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCatMeasurements(
        [FromQuery] int catId,
        [FromQuery] DateTimeOffset? minTime,
        [FromQuery] DateTimeOffset? maxTime)
    {
        var data = await _graphService.GetCatMeasurementsGraph(catId, minTime, maxTime);
        return File(data, "image/svg+xml");
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCombinedCatMeasurements(
        [FromQuery] int catId1,
        [FromQuery] int catId2,
        [FromQuery] bool sameAxis,
        [FromQuery] DateTimeOffset? minTime,
        [FromQuery] DateTimeOffset? maxTime)
    {
        var data = await _graphService.GetCombinedCatMeasurementsGraph(catId1, catId2, sameAxis, minTime, maxTime);
        return File(data, "image/svg+xml");
    }
    
    [HttpGet]
    public async Task<IActionResult> GetScaleEvent(
        [FromQuery] int scaleEventId)
    {
        var data = await _graphService.GetScaleEventGraph(scaleEventId);
        return File(data, "image/svg+xml");
    }
    
    [HttpGet]
    public async Task<IActionResult> GetToiletData(
        [FromQuery] int toiletId,
        [FromQuery] ToiletSensorValue sensorValue)
    {
        var data = await _graphService.GetToiletGraph(toiletId, sensorValue);
        return File(data, "image/svg+xml");
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCombinedToiletData(
        [FromQuery] int toiletId,
        [FromQuery] ToiletSensorValue sensorValue1,
        [FromQuery] ToiletSensorValue sensorValue2)
    {
        var data = await _graphService.GetCombinedToiletGraph(toiletId, sensorValue1, sensorValue2);
        return File(data, "image/svg+xml");
    }
}