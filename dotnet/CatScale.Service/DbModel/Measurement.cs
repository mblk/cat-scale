using System.Text.Json.Serialization;

namespace CatScale.Service.DbModel;

public class Measurement
{
    public int Id { get; set; }
    
    [JsonIgnore]
    public Cat Cat { get; set; } = null!;
    
    [JsonIgnore]
    public Toilet Toilet { get; set; } = null!;

    public DateTimeOffset Timestamp { get; set; }
    
    public double SetupTime { get; set; }
    
    public double PooTime { get; set; }
    
    public double CleanupTime { get; set; }
    
    public double CatWeight { get; set; }
    
    public double PooWeight { get; set; }
}