namespace CatScale.Domain.Model;

public class StablePhase
{
    public int Id { get; set; }

    public ScaleEvent ScaleEvent { get; set; } = null!;
    
    public DateTimeOffset Timestamp { get; set; }
    
    public double Length { get; set; }
    
    public double Value { get; set; }
}