namespace CatScale.Domain.Model;

public class Cleaning
{
    public int Id { get; set; }
    
    public int ScaleEventId { get; set; }

    public DateTimeOffset Timestamp { get; set; }
    
    public double Time { get; set; }
    
    public double Weight { get; set; }
    
    // TODO:
    // - WeightRemoved
    // - WeightAdded ?
}