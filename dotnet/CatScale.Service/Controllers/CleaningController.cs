using CatScale.Service.Model;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class CleaningController : ControllerBase
{
    private readonly ILogger<MeasurementController> _logger;
    private readonly string? _influxToken;
    private readonly string? _influxOrg;
    private readonly string? _influxBucket;

    public CleaningController(ILogger<MeasurementController> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        _influxToken = configuration.GetValue<string>("InfluxDB:Token");
        _influxOrg = configuration.GetValue<string>("InfluxDB:Org");
        _influxBucket = configuration.GetValue<string>("InfluxDB:Bucket");
    }
    
    [HttpPost]
    public IActionResult Post([FromBody] Cleaning cleaning)
    {
        _logger.LogInformation("New cleaning 1 {cleaning}", cleaning);

         var point = PointData.Measurement("cleaning")
             .Field("cleaning_time", cleaning.CleaningTime)
             .Field("cleaning_weight", cleaning.CleaningWeight)
             .Timestamp(cleaning.TimeStamp, WritePrecision.Ns);

         using var client = new InfluxDBClient("http://Media:8086", _influxToken);
         using var write = client.GetWriteApi();
         write.WritePoint(point, _influxBucket, _influxOrg);
        
        return Ok();
    }
}