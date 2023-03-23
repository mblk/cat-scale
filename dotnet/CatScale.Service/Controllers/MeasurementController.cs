using CatScale.Service.Model;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class MeasurementController : ControllerBase
{
    private readonly ILogger<MeasurementController> _logger;
    private readonly string? _influxToken;
    private readonly string? _influxOrg;
    private readonly string? _influxBucket;

    public MeasurementController(ILogger<MeasurementController> logger, IConfiguration configuration)
    {
        _logger = logger;

        _influxToken = configuration.GetValue<string>("InfluxDB:Token");
        _influxOrg = configuration.GetValue<string>("InfluxDB:Org");
        _influxBucket = configuration.GetValue<string>("InfluxDB:Bucket");
    }

    [HttpPost]
    public IActionResult Post([FromBody] Measurement measurement)
    {
        _logger.LogInformation("New measurement 1 {measurement}", measurement);

        const double mFilou = 7300d;
        const double mFelix = 6000d;
        
        var dmFilou = Math.Abs(measurement.CatWeight - mFilou);
        var dmFelix = Math.Abs(measurement.CatWeight - mFelix);

        var cat = dmFilou < dmFelix ? "filou" : "felix";

        var point = PointData.Measurement("poo")
            .Tag("cat", cat)
            .Field("setup_time", measurement.SetupTime)
            .Field("poo_time", measurement.PooTime)
            .Field("cleanup_time", measurement.CleanupTime)
            .Field("cat_weight", measurement.CatWeight)
            .Field("poo_weight", measurement.PooWeight)
            .Timestamp(measurement.TimeStamp, WritePrecision.Ns);

        using var client = new InfluxDBClient("http://Media:8086", _influxToken);
        using var write = client.GetWriteApi();
        write.WritePoint(point, _influxBucket, _influxOrg);
        
        return Ok();
    }
}