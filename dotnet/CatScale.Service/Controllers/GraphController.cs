using System.Diagnostics;
using System.Globalization;
using System.Text;
using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Model.ScaleEvent;
using InfluxDB.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class GraphController : ControllerBase
{
    private readonly ILogger<GraphController> _logger;
    private readonly CatScaleContext _dbContext;

    public GraphController(ILogger<GraphController> logger, CatScaleContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
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
                .AppendLine($"set terminal svg size 1200,800 font \"Helvetica,16\"")

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
        const string addr = "http://152.89.94.37:8086";
        const string token = "aioQHYpnjgKBivH1k2YH9wy5o4vNABWj2x_WDjz2Y59t0g7t-dkRCk04PldsUqVmwCLXQVJ0HliFZwAmyszJ0g==";
        const string org = "catorg";
        const string bucket = "catbucket";
        
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