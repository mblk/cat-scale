namespace CatScale.Domain.Model;

public class StablePhase
{
    public int Id { get; set; }

    public int ScaleEventId { get; set; }
   
    /// <summary>
    /// End of the stable phase.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    public double Length { get; set; }
    
    public double Value { get; set; }
}