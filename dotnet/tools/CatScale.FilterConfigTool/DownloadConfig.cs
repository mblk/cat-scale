using CatScale.FilterConfigTool.Persistence;

namespace CatScale.FilterConfigTool;

public class DownloadConfig
{
    public InfluxDbConfig InfluxDb { get; }

    public DownloadConfig(InfluxDbConfig influxDb)
    {
        InfluxDb = influxDb ?? throw new ArgumentException($"Missing config section {nameof(InfluxDb)}");
    }
}