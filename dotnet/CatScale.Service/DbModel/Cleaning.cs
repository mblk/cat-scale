using System.Text.Json.Serialization;

namespace CatScale.Service.DbModel;

public class Cleaning
{
    public int Id { get; set; }
    
    [JsonIgnore]
    public Toilet Toilet { get; set; } = null!;
    
    public DateTimeOffset Timestamp { get; set; }
    
    public double Time { get; set; }
    
    public double Weight { get; set; }
}