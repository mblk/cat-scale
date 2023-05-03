using System.Text;
using InfluxDB.Client;

namespace CatScale.ReprocessTool;

public class InfluxReader
{
    private readonly InfluxDbConfig _config;

    public InfluxReader(InfluxDbConfig config)
    {
        _config = config;
    }
    
    public async Task<(DateTimeOffset,double)[]> GetWeightDataFromInflux(DateTimeOffset start, DateTimeOffset end)
    {
        var options = new InfluxDBClientOptions.Builder()
            .Url(_config.Uri)
            .AuthenticateToken(_config.Token)
            .TimeOut(TimeSpan.FromSeconds(60))
            .Build();
        
        using var client = new InfluxDBClient(options);

        var startDateTimeString = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endDateTimeString = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        
        var flux = new StringBuilder()
            .AppendLine($"from(bucket: \"{_config.Bucket}\")")
            .AppendLine($"|> range(start: {startDateTimeString}, stop: {endDateTimeString})")
            .AppendLine("|> filter(fn: (r) => r[\"_measurement\"] == \"scales\")")
            .AppendLine("|> filter(fn: (r) => r[\"_field\"] == \"weight_raw\")")
            .ToString();
        
        var queryApi = client.GetQueryApi();
        
        var tables = await queryApi.QueryAsync(flux, _config.Org);

        int estimatedCount = (int)(end - start).TotalSeconds * 15;
        var result = new List<(DateTimeOffset, double)>(estimatedCount);
        
        tables.ForEach(table =>
        {
            table.Records.ForEach(record =>
            {
                DateTime? time = record.GetTimeInDateTime();
                object? value = record.GetValueByKey("_value");
                
                if (time.HasValue && value is double dbl)
                    result.Add((time.Value, dbl));
            });
        });

        if (result.Any(t => t.Item1.Offset != TimeSpan.Zero))
            throw new Exception($"DateTime.Kind != Utc");
        
        return result.ToArray();
    }
}