using System.Diagnostics;
using System.Globalization;
using System.Text;
using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using InfluxDB.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class GraphController : ControllerBase
{
    private readonly ILogger<GraphController> _logger;
    private readonly CatScaleContext _dbContext;

    private readonly string _influxUrl;
    private readonly string _influxToken;
    private readonly string _influxOrg;
    private readonly string _influxBucket;
    
    public GraphController(ILogger<GraphController> logger, CatScaleContext dbContext, IConfiguration configuration)
    {
        _logger = logger;
        _dbContext = dbContext;

        _influxUrl = configuration["Influx:Url"] ?? throw new ArgumentException("missing config Influx:Url");
        _influxToken = configuration["Influx:Token"] ?? throw new ArgumentException("missing config Influx:Token");
        _influxOrg = configuration["Influx:Org"] ?? throw new ArgumentException("missing config Influx:Org");
        _influxBucket = configuration["Influx:Bucket"] ?? throw new ArgumentException("missing config Influx:Bucket");
    }

    [HttpGet]
    public async Task<IActionResult> GetCatMeasurements(int catId)
    {
        // TODO create one graph for all cats instead?
        // TODO include measurements and weights?
        
        var cat = await _dbContext.Cats
            .Include(c => c.Measurements)
            .Include(c => c.Weights)
            .SingleOrDefaultAsync(c => c.Id == catId);
        if (cat is null)
            return NotFound("Cat does not exist");

        var measurements = cat.Measurements.OrderBy(m => m.Timestamp)
            .ToArray();
        
        var sb = new StringBuilder();
        
        foreach (var m in measurements)
            sb.AppendLine($"{m.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ},{m.CatWeight:F1}");

        var csvData = sb.ToString();

        var minTimestamp = measurements.Min(m => m.Timestamp);
        var maxTimestamp = measurements.Max(m => m.Timestamp);
        var minWeight = measurements.Min(m => m.CatWeight);
        var maxWeight = measurements.Max(m => m.CatWeight);

        var inputFile = $"temp/cat-{catId}.csv";
        var outputFile = $"temp/cat-{catId}.svg";
        var gnuplotFile = $"temp/cat-{catId}.gnuplot";
        
        var gnuplotConfig = CreateGnuplotConfigForCatMeasurements(minTimestamp, maxTimestamp, minWeight, maxWeight, inputFile, outputFile, cat.Name);

        if (!Directory.Exists("temp"))
            Directory.CreateDirectory("temp");
        
        await System.IO.File.WriteAllTextAsync(inputFile, csvData);
        await System.IO.File.WriteAllTextAsync(gnuplotFile, gnuplotConfig);
        
        var process = Process.Start("gnuplot", gnuplotFile);
        await process.WaitForExitAsync();

        var image = System.IO.File.OpenRead(outputFile);
        return File(image, "image/svg+xml");
    }
    
    private string CreateGnuplotConfigForCatMeasurements(DateTimeOffset start, DateTimeOffset end, double minValue, double maxValue, string inputFile, string outputFile, string name)
    {
        string startString = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string endString = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        string minValueStr = minValue.ToString("F1", CultureInfo.InvariantCulture);
        string maxValueStr = maxValue.ToString("F1", CultureInfo.InvariantCulture);
        
        string yRangeMinStr = (minValue - 100).ToString("F1", CultureInfo.InvariantCulture);
        string yRangeMaxStr = (maxValue + 100).ToString("F1", CultureInfo.InvariantCulture);
        
        var sb = new StringBuilder()
                // .AppendLine($"set terminal png size 1200,800 font \"Verdana,10\"")
                .AppendLine($"set terminal svg size 600,300 font \"Helvetica,12\"")

                // TODO font?
                // TODO output as pdf or postscript ?
                
                .AppendLine($"set output '{outputFile}'")
                .AppendLine($"set xdata time")
                .AppendLine($"set timefmt \"%Y-%m-%dT%H:%M:%SZ\"")
                .AppendLine($"set xrange [\"{startString}\":\"{endString}\"]")
                .AppendLine($"set yrange [ {yRangeMinStr} : {yRangeMaxStr}  ]")
                .AppendLine($"set format x \"%d.%m\"")
                .AppendLine($"set datafile separator \",\"")
                .AppendLine($"set grid")
            ;

        // plot
        sb.AppendLine($"plot \"{inputFile}\" using 1:2 title '{name}' with lines");
        
        return sb.ToString();
    }
    
    [HttpGet]
    public async Task<IActionResult> GetScaleEvent(int scaleEventId)
    {
        _logger.LogInformation($"Get graph for scale event {scaleEventId}");

        var scaleEvent = await _dbContext.ScaleEvents
            .AsNoTracking()
            .Include(x => x.StablePhases)
            .SingleOrDefaultAsync(x => x.Id == scaleEventId);
        if (scaleEvent is null)
            return NotFound();

        try
        {
            if (!Directory.Exists("temp"))
                Directory.CreateDirectory("temp");
            
            var extraTime = TimeSpan.FromSeconds(30);
            var start = scaleEvent.StartTime - extraTime;
            var end = scaleEvent.EndTime + extraTime;
            
            var weightData = await GetWeightDataFromInflux(start, end);
            var csvData = ConvertDataToCsv(weightData);
            await System.IO.File.WriteAllTextAsync($"temp/data-{scaleEventId}.csv", csvData);

            double minValue = weightData.Min(x => x.Item2);
            double maxValue = weightData.Max(x => x.Item2);
            
            var gnuplotConfig = CreateGnuplotConfig(start, end, minValue, maxValue, scaleEvent);
            await System.IO.File.WriteAllTextAsync($"temp/data-{scaleEventId}.gnuplot", gnuplotConfig);

            var process = Process.Start("gnuplot", $"temp/data-{scaleEventId}.gnuplot");
            await process.WaitForExitAsync();

            var image = System.IO.File.OpenRead($"temp/data-{scaleEventId}.svg");
            return File(image, "image/svg+xml");
        }
        catch (Exception e)
        {
            _logger.LogError(e.ToString());
            throw;
        }
    }

    private string CreateGnuplotConfig(DateTimeOffset start, DateTimeOffset end, double minValue, double maxValue, ScaleEvent scaleEvent)
    {
        string startString = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string endString = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        string eventStartStr = scaleEvent.StartTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string eventEndStr = scaleEvent.EndTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        string minValueStr = minValue.ToString("F1", CultureInfo.InvariantCulture);
        string maxValueStr = maxValue.ToString("F1", CultureInfo.InvariantCulture);
        
        string yRangeMinStr = (minValue - 500).ToString("F1", CultureInfo.InvariantCulture);
        string yRangeMaxStr = (maxValue + 500).ToString("F1", CultureInfo.InvariantCulture);
        
        var sb = new StringBuilder()
                // .AppendLine($"set terminal png size 1200,800 font \"Verdana,10\"")
                .AppendLine($"set terminal svg size 1200,600 font \"Helvetica,16\"")

                // TODO font?
                // TODO output as pdf or postscript ?
                
                .AppendLine($"set output 'temp/data-{scaleEvent.Id}.svg'")
                .AppendLine($"set xdata time")
                .AppendLine($"set timefmt \"%Y-%m-%dT%H:%M:%SZ\"")
                .AppendLine($"set xrange [\"{startString}\":\"{endString}\"]")
                .AppendLine($"set yrange [ {yRangeMinStr} : {yRangeMaxStr}  ]")
                .AppendLine($"set format x \"%H:%M:%S\"")
                .AppendLine($"set datafile separator \",\"")

                .AppendLine($"set grid")
                
                .AppendLine($"set style rect fc lt -1 fs solid 0.15 noborder")
                .AppendLine($"set object 1 rect from \"{eventStartStr}\",{minValueStr} to \"{eventEndStr}\",{maxValueStr}")
                
                .AppendLine($"set style rect fc lt 3 fs solid 1.0 noborder")
            ;

        bool labelOffset = true;
        int objId = 2;
        foreach (var stablePhase in scaleEvent.StablePhases)
        {
            var t2 = stablePhase.Timestamp;
            var t1 = t2 - TimeSpan.FromSeconds(stablePhase.Length);
            
            var t1s = t1.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var t2s = t2.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            const int boxHeight = 200;
            const int halfBoxHeight = boxHeight / 2;
            
            var v1 = stablePhase.Value - halfBoxHeight;
            var v2 = v1 + boxHeight;

            var v1s = v1.ToString("F1", CultureInfo.InvariantCulture);
            var v2s = v2.ToString("F1", CultureInfo.InvariantCulture);
            
            sb.AppendLine($"set object {objId++} rect from \"{t1s}\",{v1s} to \"{t2s}\",{v2s}");

            //var label = $"{stablePhase.Value:F1}g ({stablePhase.Length:F1}s)";
            var label = $"{stablePhase.Value:F0}g";

            var vl = v2 + (labelOffset ? halfBoxHeight : (-1.5 * boxHeight));
            var vls = vl.ToString("F1", CultureInfo.CurrentCulture);

            sb.AppendLine($"set label \"{label}\" at \"{t1s}\",{vls}");

            labelOffset = !labelOffset;
        }

        // stats
        var eventLength = (scaleEvent.EndTime - scaleEvent.StartTime).TotalSeconds;
        var eventLengthStr = $"{eventLength:F0}s";

        var centerTime = scaleEvent.StartTime.AddSeconds(eventLength / 2);
        var centerTimeStr = centerTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        var statY = maxValue + 80;
        var statYStr = statY.ToString("F1", CultureInfo.InvariantCulture);
        
        sb.AppendLine($"set label \"{eventLengthStr}\" at \"{centerTimeStr}\",{statYStr}");

        // plot
        sb.AppendLine($"plot \"temp/data-{scaleEvent.Id}.csv\" using 1:2 title '' with lines");
        
        
        return sb.ToString();
    }

    private string ConvertDataToCsv(IEnumerable<(DateTime, double)> data)
    {
        var sb = new StringBuilder();

        foreach (var (time, value) in data)
            sb.AppendLine($"{time:yyyy-MM-ddTHH:mm:ss.fffZ},{value:F1}");
        
        return sb.ToString();
    }

    private async Task<IEnumerable<(DateTime,double)>> GetWeightDataFromInflux(DateTimeOffset start, DateTimeOffset end)
    {
        string addr = _influxUrl;
        string token = _influxToken;
        string org = _influxOrg;
        string bucket = _influxBucket;
        
        using var client = new InfluxDBClient(addr, token);

        var startDateTimeString = start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var endDateTimeString = end.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        
        var flux = new StringBuilder()
            .AppendLine($"from(bucket: \"{bucket}\")")
            .AppendLine($"|> range(start: {startDateTimeString}, stop: {endDateTimeString})")
            .AppendLine("|> filter(fn: (r) => r[\"_measurement\"] == \"scales\")")
            .AppendLine("|> filter(fn: (r) => r[\"_field\"] == \"weight\")")
            .ToString();
        
        var queryApi = client.GetQueryApi();
        
        var tables = await queryApi.QueryAsync(flux, org);

        int estimatedCount = (int)(end - start).TotalSeconds * 15;
        var result = new List<(DateTime, double)>(estimatedCount);
        
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

        return result;
    }
}