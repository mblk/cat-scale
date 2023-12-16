using System.Diagnostics;
using System.Text;
using CatScale.Service.Model.Toilet;
using InfluxDB.Client;

namespace CatScale.Service.Services;

public interface IInfluxService
{
    Task<IEnumerable<(DateTimeOffset, double)>> GetRawData(int toiletId, DateTimeOffset start, DateTimeOffset end, ToiletSensorValue value);

    Task<IEnumerable<(DateTimeOffset, double)>> GetAggregatedData(int toiletId, DateTimeOffset start, DateTimeOffset end, ToiletSensorValue value);
}

public class InfluxService : IInfluxService
{
    private readonly ILogger<InfluxService> _logger;
    
    private readonly string _influxUrl;
    private readonly string _influxToken;
    private readonly string _influxOrg;
    private readonly string _influxBucket;
    
    public InfluxService(ILogger<InfluxService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        _influxUrl = configuration["Influx:Url"] ?? throw new ArgumentException("missing config Influx:Url");
        _influxToken = configuration["Influx:Token"] ?? throw new ArgumentException("missing config Influx:Token");
        _influxOrg = configuration["Influx:Org"] ?? throw new ArgumentException("missing config Influx:Org");
        _influxBucket = configuration["Influx:Bucket"] ?? throw new ArgumentException("missing config Influx:Bucket");
    }
    
    public async Task<IEnumerable<(DateTimeOffset, double)>> GetRawData(int toiletId, DateTimeOffset start, DateTimeOffset end, ToiletSensorValue value)
    {
        // TODO filter by toiletId
        
        var flux = new StringBuilder()
            .AppendLine($"from(bucket: \"{_influxBucket}\")")
            .AppendLine($"|> range(start: {ConvertDateTimeOffsetToString(start)}, stop: {ConvertDateTimeOffsetToString(end)})")
            .AppendLine($"|> filter(fn: (r) => r[\"_measurement\"] == \"scales\")")
            .AppendLine($"|> filter(fn: (r) => r[\"_field\"] == \"{GetFieldName(value)}\")")
            .ToString();
        
        return await GetDataFromFluxQuery(flux);
    }

    public async Task<IEnumerable<(DateTimeOffset, double)>> GetAggregatedData(int toiletId, DateTimeOffset start, DateTimeOffset end,
        ToiletSensorValue value)
    {
        // TODO filter by toiletId
        
        var flux = new StringBuilder()
            .AppendLine($"from(bucket: \"{_influxBucket}\")")
            .AppendLine($"|> range(start: {ConvertDateTimeOffsetToString(start)}, stop: {ConvertDateTimeOffsetToString(end)})")
            .AppendLine($"|> filter(fn: (r) => r[\"_measurement\"] == \"scales\")")
            .AppendLine($"|> filter(fn: (r) => r[\"_field\"] == \"{GetFieldName(value)}\")")
            .AppendLine($"|> aggregateWindow(every: 1m, fn: mean, createEmpty: false)")
            .AppendLine($"|> yield(name: \"mean\")") // Optional?
            .ToString();

        return await GetDataFromFluxQuery(flux);
    }
    
    private async Task<IEnumerable<(DateTimeOffset, double)>> GetDataFromFluxQuery(string fluxQuery)
    {
        using var client = new InfluxDBClient(_influxUrl, _influxToken);
        
        var queryApi = client.GetQueryApi();
        
        var tables = await queryApi.QueryAsync(fluxQuery, _influxOrg);

        var result = new List<(DateTimeOffset, double)>();
        
        tables.ForEach(table =>
        {
            table.Records.ForEach(record =>
            {
                DateTime? time = record.GetTimeInDateTime();
                object? val = record.GetValueByKey("_value");

                if (time.HasValue && val is double dbl)
                {
                    Debug.Assert(time.Value.Kind == DateTimeKind.Utc);
                    result.Add((new DateTimeOffset(time.Value), dbl));
                }
            });
        });

        return result;
    }

    private static string ConvertDateTimeOffsetToString(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    private static string GetFieldName(ToiletSensorValue value)
    {
        return value switch
        {
            ToiletSensorValue.Weight => "weight",
            ToiletSensorValue.RawWeight => "weight_raw",
            ToiletSensorValue.Temperature => "temperature",
            ToiletSensorValue.Humidity => "humidity",
            ToiletSensorValue.Pressure => "pressure",
            ToiletSensorValue.Co2 => "co2",
            ToiletSensorValue.Tvoc => "tvoc",
            _ => throw new ArgumentException($"Invalid ToiletSensorValue: {value}")
        };
    }
}