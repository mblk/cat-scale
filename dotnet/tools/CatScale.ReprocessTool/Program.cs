using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CatScale.Service.Model.ScaleEvent;

namespace CatScale.ReprocessTool;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine($"Hello from Reprocess Tool ({IntPtr.Size*8}bit)");

        try
        {
            if (!TryParseConfig(args, out var config, out var startTime))
                return;

            var weightData = await ReadDataFromInflux(config.InfluxDb, startTime);

            var scaleEvents = ProcessData(weightData);

            await PostScaleEvents(config.Service, scaleEvents);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Internal error: {e}");
        }
        
        Console.WriteLine("Bye from Reprocess Tool");
    }
    
    private static bool TryParseConfig(string[] args, [NotNullWhen(true)] out Config? config, out DateTimeOffset startTime)
    {
        config = null;
        startTime = DateTimeOffset.MaxValue;
        
        if (args.Length != 2)
        {
            Console.WriteLine($"Usage: CatScale.ReprocessTool config_file start_time");
            Console.WriteLine($"eg:    CatScale.ReprocessTool config.json 2023-05-20");
            return false;
        }
        
        // Argument 1: Config file
        var configFileName = args[0];
        if (!File.Exists(configFileName))
        {
            Console.Error.WriteLine($"Config file '{configFileName}' does not exist");
            return false;
        }

        config = JsonSerializer.Deserialize<Config>(File.Open(configFileName, FileMode.Open));
        if (config is null)
        {
            Console.Error.WriteLine($"Config is incomplete");
            return false;
        }

        // Argument 2: Start time
        var startTimeString = args[1];
        if (!DateTimeOffset.TryParseExact(startTimeString, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out startTime))
        {
            Console.Error.WriteLine($"Can't parse start time");
            return false;
        }

        return true;
    }

    private static async Task<(DateTimeOffset, double)[]> ReadDataFromInflux(InfluxDbConfig config, DateTimeOffset startTime)
    {
        Console.WriteLine($"Loading data from InfluxDB ...");
        
        var endTime = startTime.AddDays(1);
        Console.WriteLine($"Start time: {startTime}");
        Console.WriteLine($"End time:   {endTime}");
            
        var influxReader = new InfluxReader(config);
        var weightData = await influxReader.GetWeightDataFromInflux(startTime, endTime);
        Console.WriteLine($"Samples: {weightData.Length} (should be ~ {24 * 3600 * 10})");

        return weightData;
    }
    
    private static NewScaleEvent[] ProcessData((DateTimeOffset,double)[] weightData)
    {
        Console.WriteLine($"Processing data ...");
            
        var eventBuffer = new EventBuffer();
        
        NativeFilterLib.RegisterHandlers(eventBuffer.StartOfEvent, eventBuffer.StablePhase, eventBuffer.EndOfEvent);
        NativeFilterLib.InitFilterCascade();
        
        for (int i = 1; i < weightData.Length; i++)
        {
            (DateTimeOffset t0, _) = weightData[i - 1];
            (DateTimeOffset t1, double value) = weightData[i];
        
            double dt = (t1 - t0).TotalSeconds;
            eventBuffer.CurrentTime = t1;
            
            _ = NativeFilterLib.ProcessValueInFilterCascade(value, dt);
        }
        
        //eventBuffer.Dump();

        return eventBuffer.ScaleEvents.ToArray();
    }

    private static async Task PostScaleEvents(ServiceConfig config, IEnumerable<NewScaleEvent> scaleEvents)
    {
        Console.WriteLine($"Posting scale events to service ...");

        var httpClient = new HttpClient()
        {
            BaseAddress = new Uri(config.Uri)
        };
        httpClient.DefaultRequestHeaders.Add("Authorization", "ApiKey");
        httpClient.DefaultRequestHeaders.Add("ApiKey", config.Token);

        foreach (var scaleEvent in scaleEvents)
        {
            Console.WriteLine($"Posting {scaleEvent} ...");
            var response = await httpClient.PostAsJsonAsync($"api/ScaleEvent/Create", scaleEvent);
            Console.WriteLine($"=> {response.StatusCode}");
        }
    }
}