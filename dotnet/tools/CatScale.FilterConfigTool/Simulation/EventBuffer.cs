namespace CatScale.FilterConfigTool.Simulation;

public record NewStablePhase(DateTimeOffset Timestamp, double Length, double Weight);
public record NewScaleEvent(DateTimeOffset StartTime, DateTimeOffset EndTime, NewStablePhase[] StablePhases);

public class EventBuffer
{
    public DateTimeOffset CurrentTime { get; set; }
    
    private bool _isInEvent = false;
    private DateTimeOffset _eventStartTime = DateTimeOffset.MinValue;
    private readonly List<NewStablePhase> _stablePhases = new List<NewStablePhase>();
    private readonly List<NewScaleEvent> _scaleEvents = new List<NewScaleEvent>();

    public IEnumerable<NewScaleEvent> ScaleEvents => _scaleEvents;
    
    public EventBuffer()
    {
    }
    
    public void StartOfEvent()
    {
        _isInEvent = true;
        _eventStartTime = CurrentTime;
        _stablePhases.Clear();
    }

    public void StablePhase(double length, double value)
    {
        if (_isInEvent)
            _stablePhases.Add(new NewStablePhase(CurrentTime, length, value));
        else
            Console.Error.WriteLine($"Trying to add stable phase outside of event!");
    }
    
    public void EndOfEvent()
    {
        _scaleEvents.Add(new NewScaleEvent(_eventStartTime, CurrentTime,
            _stablePhases.ToArray()));
        _isInEvent = false;
    }

    public void Dump()
    {
        foreach (var scaleEvent in _scaleEvents)
        {
            Console.WriteLine($"*** scale event ***");
            Console.WriteLine($"start: {scaleEvent.StartTime}");
            Console.WriteLine($"end: {scaleEvent.EndTime}");
            foreach (var stablePhase in scaleEvent.StablePhases)
                Console.WriteLine($"stable phase: {stablePhase}");
            Console.WriteLine("");
        }
    }
}