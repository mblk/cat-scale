using JetBrains.Annotations;

namespace CatScale.Service.Model.Cleaning;

[PublicAPI]
public class CleaningDto
{
    public int Id { get; set; }
   
    public DateTimeOffset Timestamp { get; set; }
    
    public double Time { get; set; }
    
    public double Weight { get; set; }
}