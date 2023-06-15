using System.Diagnostics;
using System.Globalization;
using System.Text;
using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Model.Toilet;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Services;

public interface IGraphService
{
    Task<Stream> GetCatMeasurementsGraph(int catId, DateTimeOffset? minTime, DateTimeOffset? maxTime, bool includeTemperature);

    Task<Stream> GetCombinedCatMeasurementsGraph(int catId1, int catId2, bool sameAxis, DateTimeOffset? minTime, DateTimeOffset? maxTime);
    
    Task<Stream> GetToiletGraph(int toiletId, ToiletSensorValue value);
    
    Task<Stream> GetCombinedToiletGraph(int toiletId, ToiletSensorValue value1, ToiletSensorValue value2);
    
    Task<Stream> GetScaleEventGraph(int scaleEventId);
}

public class GraphService : IGraphService
{
    private readonly ILogger<GraphService> _logger;
    private readonly CatScaleContext _dbContext;
    private readonly IInfluxService _influxService;

    public GraphService(ILogger<GraphService> logger, CatScaleContext dbContext, IInfluxService influxService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _influxService = influxService;
    }

    public async Task<Stream> GetCatMeasurementsGraph(int catId, DateTimeOffset? minTime, DateTimeOffset? maxTime, bool includeTemperature)
    {
        var cat = await _dbContext.Cats
                      .AsNoTracking()
                      .SingleOrDefaultAsync(c => c.Id == catId)
                  ?? throw new ArgumentException("Cat does not exist");
        
        if (includeTemperature)
        {
            var measurementsWithTemperature = _dbContext.ScaleEvents
                .AsNoTracking()
                .Where(e => minTime == null || e.StartTime >= minTime)
                .Where(e => maxTime == null || e.EndTime <= maxTime)
                .Include(e => e.Measurement)
                .Where(e => e.Measurement != null)
                .Where(e => e.Measurement!.CatId == catId)
                .OrderBy(e => e.Measurement!.Timestamp)
                .ToArray()
                .Select(e => (e.Measurement!.Timestamp, e.Measurement.CatWeight, e.Temperature))
                .ToArray();

            var weightData = measurementsWithTemperature
                .Select(m => (m.Timestamp, m.CatWeight))
                .ToArray();

            var temperatureData = measurementsWithTemperature
                .Where(m => m.Temperature != 0d)
                .Select(m => (m.Timestamp, m.Temperature))
                .ToArray();
            
            var minTimestamp = weightData.Min(m => m.Timestamp);
            var maxTimestamp = weightData.Max(m => m.Timestamp);
            var minWeight = weightData.Min(m => m.CatWeight);
            var maxWeight = weightData.Max(m => m.CatWeight);
            var minTemperature = temperatureData.Min(m => m.Temperature);
            var maxTemperature = temperatureData.Max(m => m.Temperature);

            var inputFile1 = $"temp/cat-{catId}-1.csv";
            var inputFile2 = $"temp/cat-{catId}-2.csv";
            var outputFile = $"temp/cat-{catId}.svg";
            var gnuplotFile = $"temp/cat-{catId}.gnuplot";

            string csvData1 = ConvertDateTimeOffsetDoubleToCsv(weightData);
            string csvData2 = ConvertDateTimeOffsetDoubleToCsv(temperatureData);
            string gnuplotConfig = CreateGnuplotConfigCombinedGraph(minTimestamp, maxTimestamp, minWeight, maxWeight,
                minTemperature, maxTemperature, inputFile1, inputFile2, outputFile, cat.Name, "Temperature");

            if (!Directory.Exists("temp"))
                Directory.CreateDirectory("temp");
            
            await File.WriteAllTextAsync(inputFile1, csvData1);
            await File.WriteAllTextAsync(inputFile2, csvData2);
            await File.WriteAllTextAsync(gnuplotFile, gnuplotConfig);
            await Process.Start("gnuplot", gnuplotFile).WaitForExitAsync();

            return File.OpenRead(outputFile);
        }
        else
        {
            var measurements = _dbContext.ScaleEvents
                .AsNoTracking()
                .Where(e => minTime == null || e.StartTime >= minTime)
                .Where(e => maxTime == null || e.EndTime <= maxTime)
                .Include(e => e.Measurement)
                .Where(e => e.Measurement != null)
                .Where(e => e.Measurement!.CatId == catId)
                .OrderBy(e => e.Measurement!.Timestamp)
                .ToArray()
                .Select(e => (e.Measurement!.Timestamp, e.Measurement.CatWeight))
                .ToArray();

            var weightData = measurements
                .Select(m => (m.Timestamp, m.CatWeight))
                .ToArray();

            var minTimestamp = weightData.Min(m => m.Timestamp);
            var maxTimestamp = weightData.Max(m => m.Timestamp);
            var minWeight = weightData.Min(m => m.CatWeight);
            var maxWeight = weightData.Max(m => m.CatWeight);

            var inputFile1 = $"temp/cat-{catId}-3.csv";
            var outputFile = $"temp/cat-{catId}.svg";
            var gnuplotFile = $"temp/cat-{catId}.gnuplot";

            string csvData1 = ConvertDateTimeOffsetDoubleToCsv(weightData);
            string gnuplotConfig = CreateGnuplotConfigSimpleGraph(minTimestamp, maxTimestamp,
                minWeight, maxWeight, inputFile1, outputFile, cat.Name);

            if (!Directory.Exists("temp"))
                Directory.CreateDirectory("temp");
            
            await File.WriteAllTextAsync(inputFile1, csvData1);
            await File.WriteAllTextAsync(gnuplotFile, gnuplotConfig);
            await Process.Start("gnuplot", gnuplotFile).WaitForExitAsync();

            return File.OpenRead(outputFile);
        }
    }
    
    public async Task<Stream> GetCombinedCatMeasurementsGraph(int catId1, int catId2, bool sameAxis, DateTimeOffset? minTime, DateTimeOffset? maxTime)
    {
        var cat1 = await _dbContext.Cats
                       .AsNoTracking()
                      .Include(c => c.Measurements)
                      .Include(c => c.Weights)
                      .SingleOrDefaultAsync(c => c.Id == catId1)
                  ?? throw new ArgumentException("Cat1 does not exist");
        var cat2 = await _dbContext.Cats
                       .AsNoTracking()
                       .Include(c => c.Measurements)
                       .Include(c => c.Weights)
                       .SingleOrDefaultAsync(c => c.Id == catId2)
                   ?? throw new ArgumentException("Cat2 does not exist");

        var measurements1 = cat1.Measurements
            .Where(m => minTime == null || m.Timestamp >= minTime)
            .Where(m => maxTime == null || m.Timestamp <= maxTime)
            .OrderBy(m => m.Timestamp)
            .ToArray();
        var measurements2 = cat2.Measurements
            .Where(m => minTime == null || m.Timestamp >= minTime)
            .Where(m => maxTime == null || m.Timestamp <= maxTime)
            .OrderBy(m => m.Timestamp)
            .ToArray();

        var combinedMeasurements = measurements1
            .Concat(measurements2)
            .ToArray();
        
        var minTimestamp = combinedMeasurements.Min(m => m.Timestamp);
        var maxTimestamp = combinedMeasurements.Max(m => m.Timestamp);

        double minWeight1, maxWeight1, minWeight2, maxWeight2;
        if (sameAxis)
        {
            minWeight1 = minWeight2 = combinedMeasurements.Min(m => m.CatWeight);
            maxWeight1 = maxWeight2 = combinedMeasurements.Max(m => m.CatWeight);
        }
        else
        {
            minWeight1 = measurements1.Min(m => m.CatWeight);
            maxWeight1 = measurements1.Max(m => m.CatWeight);
            minWeight2 = measurements2.Min(m => m.CatWeight);
            maxWeight2 = measurements2.Max(m => m.CatWeight);
        }
        
        var filePrefix = $"temp/cat-combined-{catId1}-{catId2}-{sameAxis}"; 
        var inputFile1 = $"{filePrefix}-1.csv";
        var inputFile2 = $"{filePrefix}-2.csv";
        var outputFile = $"{filePrefix}.svg";
        var gnuplotFile = $"{filePrefix}.gnuplot";

        string csvData1 = ConvertMeasurementsToCsv(measurements1);
        string csvData2 = ConvertMeasurementsToCsv(measurements2);

        string gnuplotConfig = CreateGnuplotConfigCombinedGraph(minTimestamp, maxTimestamp, minWeight1, maxWeight1,
            minWeight2, maxWeight2,
            inputFile1, inputFile2, outputFile, cat1.Name, cat2.Name);

        if (!Directory.Exists("temp"))
            Directory.CreateDirectory("temp");
        
        await File.WriteAllTextAsync(inputFile1, csvData1);
        await File.WriteAllTextAsync(inputFile2, csvData2);
        await File.WriteAllTextAsync(gnuplotFile, gnuplotConfig);
        await Process.Start("gnuplot", gnuplotFile).WaitForExitAsync();

        return File.OpenRead(outputFile);
    }

    public async Task<Stream> GetToiletGraph(int toiletId, ToiletSensorValue value)
    {
        var endTime = DateTimeOffset.Now;
        var startTime = endTime.AddDays(-1);

        var data = (await _influxService.GetAggregatedData(startTime, endTime, value)).ToArray();
        double minValue = data.MinBy(x => x.Item2).Item2;
        double maxValue = data.MaxBy(x => x.Item2).Item2;

        var inputFile = $"temp/toilet-{toiletId}-{value}.csv";
        var outputFile = $"temp/toilet-{toiletId}-{value}.svg";
        var gnuplotFile = $"temp/toilet-{toiletId}-{value}.gnuplot";

        string csvData = ConvertDateTimeOffsetDoubleToCsv(data);
        string gnuplotConfig = CreateGnuplotConfigSimpleGraph(startTime, endTime, minValue, maxValue, inputFile,
            outputFile, value.ToString());

        if (!Directory.Exists("temp"))
            Directory.CreateDirectory("temp");
        
        await File.WriteAllTextAsync(inputFile, csvData);
        await File.WriteAllTextAsync(gnuplotFile, gnuplotConfig);
        await Process.Start("gnuplot", gnuplotFile).WaitForExitAsync();

        return File.OpenRead(outputFile);
    }

    public async Task<Stream> GetCombinedToiletGraph(int toiletId, ToiletSensorValue value1, ToiletSensorValue value2)
    {
        var endTime = DateTimeOffset.Now;
        var startTime = endTime.AddDays(-1);

        var data1 = (await _influxService.GetAggregatedData(startTime, endTime, value1)).ToArray();
        var data2 = (await _influxService.GetAggregatedData(startTime, endTime, value2)).ToArray();
        
        double minValue1 = data1.MinBy(x => x.Item2).Item2;
        double maxValue1 = data1.MaxBy(x => x.Item2).Item2;
        
        double minValue2 = data2.MinBy(x => x.Item2).Item2;
        double maxValue2 = data2.MaxBy(x => x.Item2).Item2;

        var filePrefix = $"temp/toilet-combined-{toiletId}-{value1}-{value2}";
        var inputFile1 = $"{filePrefix}-1.csv";
        var inputFile2 = $"{filePrefix}-2.csv";
        var outputFile = $"{filePrefix}.svg";
        var gnuplotFile = $"{filePrefix}.gnuplot";

        string csvData1 = ConvertDateTimeOffsetDoubleToCsv(data1);
        string csvData2 = ConvertDateTimeOffsetDoubleToCsv(data2);
        
        string gnuplotConfig = CreateGnuplotConfigCombinedGraph(startTime, endTime, minValue1, maxValue1, minValue2,
            maxValue2, inputFile1, inputFile2, outputFile, value1.ToString(), value2.ToString());
        
        if (!Directory.Exists("temp"))
            Directory.CreateDirectory("temp");
        
        await File.WriteAllTextAsync(inputFile1, csvData1);
        await File.WriteAllTextAsync(inputFile2, csvData2);
        await File.WriteAllTextAsync(gnuplotFile, gnuplotConfig);
        await Process.Start("gnuplot", gnuplotFile).WaitForExitAsync();

        return File.OpenRead(outputFile);
    }

    public async Task<Stream> GetScaleEventGraph(int scaleEventId)
    {
        var scaleEvent = await _dbContext.ScaleEvents
                             .AsNoTracking()
                             .Include(x => x.StablePhases)
                             .SingleOrDefaultAsync(x => x.Id == scaleEventId)
                         ?? throw new ArgumentException("Scale event does not exist");
        
        var extraTime = TimeSpan.FromSeconds(30);
        var startTime = scaleEvent.StartTime - extraTime;
        var endTime = scaleEvent.EndTime + extraTime;

        var weightData = (await _influxService.GetRawData(startTime, endTime, ToiletSensorValue.Weight)).ToArray();
        double minValue = weightData.Min(x => x.Item2);
        double maxValue = weightData.Max(x => x.Item2);
        
        var dataFileName = $"temp/data-{scaleEventId}.csv";
        var configFileName = $"temp/data-{scaleEventId}.gnuplot";
        var outputFileName = $"temp/data-{scaleEventId}.svg";
        
        string csvData = ConvertDateTimeOffsetDoubleToCsv(weightData);
        string gnuplotConfig = CreateGnuplotConfigForScaleEvent(startTime, endTime, minValue, maxValue, scaleEvent, dataFileName, outputFileName);
        
        if (!Directory.Exists("temp"))
            Directory.CreateDirectory("temp");
        
        await File.WriteAllTextAsync(dataFileName, csvData);
        await File.WriteAllTextAsync(configFileName, gnuplotConfig);
        await Process.Start("gnuplot", configFileName).WaitForExitAsync();

        return File.OpenRead(outputFileName);
    }
    
    private static string CreateGnuplotConfigSimpleGraph(DateTimeOffset start, DateTimeOffset end, double minValue, double maxValue, string inputFile, string outputFile, string name)
    {
        var timeRange = end - start;
        string xFormat = timeRange.TotalDays > 1.5 ? "%d.%m" : "%H:%M";
        
        double valueRange = maxValue - minValue;
        double valueExtra = valueRange * 0.1;

        return new StringBuilder()
            .AppendLine($"set terminal svg size 800,300 font 'Helvetica,12'")
            .AppendLine($"set output '{outputFile}'")
            .AppendLine($"set xdata time")
            .AppendLine($"set timefmt '%Y-%m-%dT%H:%M:%SZ'")
            .AppendLine($"set xrange ['{ConvertDateTimeOffsetToString(start)}':'{ConvertDateTimeOffsetToString(end)}']")
            .AppendLine($"set yrange [{ConvertDoubleToString(minValue - valueExtra)}:{ConvertDoubleToString(maxValue + valueExtra)}]")
            .AppendLine($"set format x '{xFormat}'")
            .AppendLine($"set datafile separator ','")
            .AppendLine($"set grid")
            .AppendLine($"plot '{inputFile}' using 1:2 title '{name}' with lines")
            .ToString();
    }
    
    private static string CreateGnuplotConfigCombinedGraph(DateTimeOffset start, DateTimeOffset end,
        double minValue1, double maxValue1, double minValue2, double maxValue2,
        string inputFile1, string inputFile2, string outputFile, string name1, string name2)
    {
        var timeRange = end - start;
        string xFormat = timeRange.TotalDays > 1.5 ? "%d.%m" : "%H:%M";
        
        double valueRange1 = maxValue1 - minValue1;
        double valueExtra1 = valueRange1 * 0.1;
        
        double valueRange2 = maxValue2 - minValue2;
        double valueExtra2 = valueRange2 * 0.1;

        return new StringBuilder()
            .AppendLine($"set terminal svg size 800,300 font 'Helvetica,12'")
            .AppendLine($"set output '{outputFile}'")
            .AppendLine($"set xdata time")
            .AppendLine($"set timefmt '%Y-%m-%dT%H:%M:%SZ'")
            .AppendLine($"set xrange ['{ConvertDateTimeOffsetToString(start)}':'{ConvertDateTimeOffsetToString(end)}']")
            .AppendLine($"set yrange [{ConvertDoubleToString(minValue1 - valueExtra1)}:{ConvertDoubleToString(maxValue1 + valueExtra1)}]")
            .AppendLine($"set y2range [{ConvertDoubleToString(minValue2 - valueExtra2)}:{ConvertDoubleToString(maxValue2 + valueExtra2)}]")
            .AppendLine($"set ytics nomirror")
            .AppendLine($"set y2tics")
            .AppendLine($"set ylabel '{name1}'")
            .AppendLine($"set y2label '{name2}'")
            .AppendLine($"set format x '{xFormat}'")
            .AppendLine($"set datafile separator ','")
            .AppendLine($"set grid")
            .AppendLine($"plot '{inputFile1}' using 1:2 title '{name1}' with lines axes x1y1, '{inputFile2}' using 1:2 title '{name2}' with lines axes x1y2")
            .ToString();
    }
    
    private static string CreateGnuplotConfigForScaleEvent(DateTimeOffset start, DateTimeOffset end,
        double minValue, double maxValue, ScaleEvent scaleEvent, string dataFileName, string outputFileName)
    {
        int nextObjId = 1;
        
        var sb = new StringBuilder()
            .AppendLine($"set terminal svg size 1200,600 font 'Helvetica,16'")
            .AppendLine($"set output '{outputFileName}'")
            .AppendLine($"set xdata time")
            .AppendLine($"set timefmt '%Y-%m-%dT%H:%M:%SZ'")
            .AppendLine($"set xrange ['{ConvertDateTimeOffsetToString(start)}':'{ConvertDateTimeOffsetToString(end)}']")
            .AppendLine($"set yrange [{ConvertDoubleToString(minValue - 500)}:{ConvertDoubleToString(maxValue + 500)}]")
            .AppendLine($"set format x '%H:%M:%S'")
            .AppendLine($"set datafile separator ','")
            .AppendLine($"set grid")
            .AppendLine($"set style rect fc lt -1 fs solid 0.15 noborder")
            .AppendLine($"set object {nextObjId++} rect from '{ConvertDateTimeOffsetToString(scaleEvent.StartTime)}',{ConvertDoubleToString(minValue)} to '{ConvertDateTimeOffsetToString(scaleEvent.EndTime)}',{ConvertDoubleToString(maxValue)}")
            .AppendLine($"set style rect fc lt 3 fs solid 1.0 noborder")
        ;

        // Draw box around each stable phase.
        bool labelOffset = true;
        foreach (var stablePhase in scaleEvent.StablePhases)
        {
            const int boxHeight = 200;
            const int halfBoxHeight = boxHeight / 2;
            
            DateTimeOffset t2 = stablePhase.Timestamp;
            DateTimeOffset t1 = t2 - TimeSpan.FromSeconds(stablePhase.Length);
            
            double v1 = stablePhase.Value - halfBoxHeight;
            double v2 = v1 + boxHeight;
            double vl = v2 + (labelOffset ? halfBoxHeight : (-1.5 * boxHeight));
            labelOffset = !labelOffset;
            
            sb.AppendLine($"set object {nextObjId++} rect from '{ConvertDateTimeOffsetToString(t1)}',{ConvertDoubleToString(v1)} to '{ConvertDateTimeOffsetToString(t2)}',{ConvertDoubleToString(v2)}");
            sb.AppendLine($"set label '{stablePhase.Value:F0}g' at '{ConvertDateTimeOffsetToString(t1)}',{ConvertDoubleToString(vl)}");
        }

        // stats
        var eventLength = (scaleEvent.EndTime - scaleEvent.StartTime).TotalSeconds;
        var centerTime = scaleEvent.StartTime.AddSeconds(eventLength / 2);
        var centerY = minValue + (maxValue - minValue) / 2;
        
        sb.AppendLine($"set label '{eventLength:F0}s' at '{ConvertDateTimeOffsetToString(centerTime)}',{ConvertDoubleToString(centerY)}");

        // plot
        sb.AppendLine($"plot '{dataFileName}' using 1:2 title '' with lines");
        
        return sb.ToString();
    }

    private static string ConvertDateTimeOffsetDoubleToCsv(IEnumerable<(DateTimeOffset, double)> data)
    {
        var sb = new StringBuilder();

        foreach (var (time, value) in data)
            sb.AppendLine($"{ConvertDateTimeOffsetToString(time)},{ConvertDoubleToString(value)}");
        
        return sb.ToString();
    }
    
    private static string ConvertMeasurementsToCsv(IEnumerable<Measurement> measurements)
    {
        var sb = new StringBuilder();

        foreach (var m in measurements)
        {
            sb.Append(ConvertDateTimeOffsetToString(m.Timestamp))
                .Append(",")
                .Append(ConvertDoubleToString(m.CatWeight))
                .AppendLine();
        }
        
        return sb.ToString();
    }

    private static string ConvertDateTimeOffsetToString(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }
    
    private static string ConvertDoubleToString(double value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }
}