namespace CatScale.FilterConfigTool.Simulation;

public class SimulationResult
{
    public EventBuffer EventBuffer { get; } = new();
    public List<(DateTimeOffset, double)> Output { get; } = new();
    public Dictionary<string, List<(DateTimeOffset, double)>> DebugData { get; } = new();
}

public class Simulator
{
    public Simulator() // pass settings?
    {
    }

    public SimulationResult Simulate(IReadOnlyList<(DateTimeOffset, double)> weightData)
    {
        if (!weightData.Any()) throw new ArgumentException($"{nameof(weightData)} is empty");
        
        const double prefixTime = 10.0;
        const double suffixTime = 80.0;
        const double idealDt = 0.1;
        const int prefixTicks = (int)(prefixTime / idealDt);
        const int suffixTicks = (int)(suffixTime / idealDt);
        
        var result = new SimulationResult();
        
        void debugHandler(string id, double value)
        {
            if (!result.DebugData.TryGetValue(id, out var values))
                result.DebugData.Add(id, values = new List<(DateTimeOffset, double)>());
            values.Add((result.EventBuffer.CurrentTime, value));
        }
        
        NativeFilterLib.RegisterHandlers(result.EventBuffer.StartOfEvent, result.EventBuffer.StablePhase,
            result.EventBuffer.EndOfEvent, debugHandler);
        NativeFilterLib.InitFilterCascade();

        for (var i = 0; i < prefixTicks; i++)
        {
            var (t, v) = weightData.First();
            t = t.AddSeconds(-prefixTime + i * idealDt);

            result.EventBuffer.CurrentTime = t;
            
            double filteredValue = NativeFilterLib.ProcessValueInFilterCascade(v, idealDt);
            result.Output.Add((t, filteredValue));
        }
        
        for (var i = 1; i < weightData.Count; i++)
        {
            (DateTimeOffset t0, _) = weightData[i - 1];
            (DateTimeOffset t1, double value) = weightData[i];
        
            double dt = (t1 - t0).TotalSeconds;
            result.EventBuffer.CurrentTime = t1;
           
            double filteredValue = NativeFilterLib.ProcessValueInFilterCascade(value, dt);
            result.Output.Add((t1, filteredValue));
        }
        
        for (var i = 0; i < suffixTicks; i++)
        {
            var (t, v) = weightData.Last();
            t = t.AddSeconds(i * idealDt);

            result.EventBuffer.CurrentTime = t;
            
            double filteredValue = NativeFilterLib.ProcessValueInFilterCascade(v, idealDt);
            result.Output.Add((t, filteredValue));
        }
        
        NativeFilterLib.CleanupFilterCascade();

        return result;
    }
}