namespace CatScale.Domain.Model;

public class ScaleEvent
{
    public int Id { get; set; }
    
    public int ToiletId { get; set; }
    // public Toilet Toilet { get; set; } = null!;

    public DateTimeOffset StartTime { get; set; }
    
    public DateTimeOffset EndTime { get; set; }
    
    public List<StablePhase> StablePhases { get; set; } = null!;
    
    public Measurement? Measurement { get; set; }
    
    public Cleaning? Cleaning { get; set; }
}