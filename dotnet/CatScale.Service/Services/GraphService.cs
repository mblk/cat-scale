using CatScale.Service.DbModel;
using CatScale.Service.Model.Toilet;
using CatScale.Service.Services.GraphBuilder;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Services;

public interface IGraphService
{
    Task<byte[]> GetCatMeasurementsGraph(int catId, DateTimeOffset? minTime, DateTimeOffset? maxTime);

    Task<byte[]> GetCombinedCatMeasurementsGraph(int catId1, int catId2, bool sameAxis, DateTimeOffset? minTime,
        DateTimeOffset? maxTime);

    Task<byte[]> GetToiletGraph(int toiletId, ToiletSensorValue value);

    Task<byte[]> GetCombinedToiletGraph(int toiletId, ToiletSensorValue value1, ToiletSensorValue value2);

    Task<byte[]> GetScaleEventGraph(int scaleEventId);
}

public class GraphService : IGraphService
{
    private readonly ILogger<GraphService> _logger;
    private readonly CatScaleDbContext _dbContext;
    private readonly IInfluxService _influxService;

    public GraphService(ILogger<GraphService> logger, CatScaleDbContext dbContext, IInfluxService influxService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _influxService = influxService;
    }

    public async Task<byte[]> GetCatMeasurementsGraph(int catId, DateTimeOffset? minTime, DateTimeOffset? maxTime)
    {
        var cat = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Measurements)
            .SingleOrDefaultAsync(c => c.Id == catId);
        if (cat is null) throw new ArgumentException("Cat does not exist");

        var measurements = cat.Measurements
            .Where(m => minTime == null || m.Timestamp >= minTime)
            .Where(m => maxTime == null || m.Timestamp <= maxTime)
            .OrderBy(m => m.Timestamp)
            .ToArray();

        var weightPoints = measurements
            .Select(m => new GraphDataPoint(m.Timestamp, m.CatWeight))
            .ToArray();
        var weightSet = new GraphDataSet(cat.Name, 1, weightPoints);
        var averageWeightSet = weightSet.CreateAverage(TimeSpan.FromDays(7));

        return await new GnuPlotGraphBuilder()
            .AddAxis(1, "Gewicht [g]")
            .AddDataset(weightSet)
            .AddDataset(averageWeightSet)
            .Build();
    }

    public async Task<byte[]> GetCombinedCatMeasurementsGraph(int catId1, int catId2, bool sameAxis,
        DateTimeOffset? minTime, DateTimeOffset? maxTime)
    {
        var cat1 = await _dbContext.Cats
                       .AsNoTracking()
                       .Include(c => c.Measurements)
                       .SingleOrDefaultAsync(c => c.Id == catId1)
                   ?? throw new ArgumentException("Cat1 does not exist");
        var cat2 = await _dbContext.Cats
                       .AsNoTracking()
                       .Include(c => c.Measurements)
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

        var set1 = new GraphDataSet(cat1.Name, 1,
            measurements1.Select(m => new GraphDataPoint(m.Timestamp, m.CatWeight)).ToArray());
        var set2 = new GraphDataSet(cat2.Name, sameAxis ? 1 : 2,
            measurements2.Select(m => new GraphDataPoint(m.Timestamp, m.CatWeight)).ToArray());

        var b = new GnuPlotGraphBuilder();

        if (sameAxis)
        {
            b.AddAxis(1, "Gewicht [g]");
        }
        else
        {
            b.AddAxis(1, cat1.Name);
            b.AddAxis(2, cat2.Name);
        }

        b.AddDataset(set1);
        b.AddDataset(set2);

        return await b.Build();
    }

    public async Task<byte[]> GetToiletGraph(int toiletId, ToiletSensorValue value)
    {
        var endTime = DateTimeOffset.Now;
        var startTime = endTime.AddDays(-1);

        var data = (await _influxService.GetAggregatedData(toiletId, startTime, endTime, value)).ToArray();

        var dataSet = new GraphDataSet(value.ToString(), 1,
            data.Select(x => new GraphDataPoint(x.Item1, x.Item2)).ToArray());

        return await new GnuPlotGraphBuilder()
            .AddDataset(dataSet)
            .Build();
    }

    public async Task<byte[]> GetCombinedToiletGraph(int toiletId, ToiletSensorValue value1, ToiletSensorValue value2)
    {
        var endTime = DateTimeOffset.Now;
        var startTime = endTime.AddDays(-1);

        var data1 = (await _influxService.GetAggregatedData(toiletId, startTime, endTime, value1)).ToArray();
        var data2 = (await _influxService.GetAggregatedData(toiletId, startTime, endTime, value2)).ToArray();

        var dataSet1 = new GraphDataSet(value1.ToString(), 1,
            data1.Select(x => new GraphDataPoint(x.Item1, x.Item2)).ToArray());
        var dataSet2 = new GraphDataSet(value2.ToString(), 2,
            data2.Select(x => new GraphDataPoint(x.Item1, x.Item2)).ToArray());

        return await new GnuPlotGraphBuilder()
            .AddDataset(dataSet1)
            .AddDataset(dataSet2)
            .Build();
    }

    public async Task<byte[]> GetScaleEventGraph(int scaleEventId)
    {
        var scaleEvent = await _dbContext.ScaleEvents
                             .AsNoTracking()
                             .Include(x => x.StablePhases)
                             .SingleOrDefaultAsync(x => x.Id == scaleEventId)
                         ?? throw new ArgumentException("Scale event does not exist");

        var extraTime = TimeSpan.FromSeconds(30);
        var startTime = scaleEvent.StartTime - extraTime;
        var endTime = scaleEvent.EndTime + extraTime;

        var weightData = (await _influxService.GetRawData(scaleEvent.ToiletId, startTime, endTime, ToiletSensorValue.Weight))
            .Select(x => new GraphDataPoint(x.Item1, x.Item2))
            .ToArray();

        var b = new GnuPlotGraphBuilder()
            .AddDataset(new GraphDataSet("Gewicht", 1, weightData));

        foreach (var stablePhase in scaleEvent.StablePhases)
        {
            var boxHeight = 200d;
            var halfBoxHeight = boxHeight / 2;

            var t2 = stablePhase.Timestamp;
            var t1 = t2 - TimeSpan.FromSeconds(stablePhase.Length);

            var v1 = stablePhase.Value - halfBoxHeight;
            var v2 = v1 + boxHeight;

            b.AddBox(t1, t2, v1, v2, $"{stablePhase.Value:F0}g");
        }

        return await b.Build();
    }
}