namespace CatScale.Service.Services.GraphBuilder;

public record GraphDataSet(string Name, int Axis, GraphDataPoint[] Points)
{
    public GraphDataSet CreateAverage(TimeSpan timeSpan)
    {
        var avgPoints = new GraphDataPoint[Points.Length];

        for (var i = 0; i < Points.Length; i++)
        {
            var current = Points[i];
            var sum = 0d;
            var count = 0;
            for (var prev = i; prev >= 0; prev--)
            {
                if (current.Time - Points[prev].Time > timeSpan)
                    break;
                sum += Points[prev].Value;
                count++;
            }

            var avg = sum / count;
            avgPoints[i] = current with { Value = avg };
        }

        return this with
        {
            Name = $"{Name} ({timeSpan.TotalDays:F0}d-avg)",
            Points = avgPoints,
        };
    }
}