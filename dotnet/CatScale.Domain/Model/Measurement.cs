namespace CatScale.Domain.Model;

public class Measurement
{
    public int Id { get; set; }
    
    public int CatId { get; set; }
    //public Cat Cat { get; set; } = null!;
    
    public int ScaleEventId { get; set; }
    //public ScaleEvent ScaleEvent { get; set; } = null!;

    public DateTimeOffset Timestamp { get; set; }
    
    public double SetupTime { get; set; }
    
    public double PooTime { get; set; }
    
    public double CleanupTime { get; set; }
    
    public double CatWeight { get; set; }
    
    public double PooWeight { get; set; }
}