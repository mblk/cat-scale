namespace CatScale.Domain.Model;

public class CatWeight
{
    public int Id { get; set; }
    
    public int CatId { get; set; }
    //public Cat Cat { get; set; } = null!;
    
    public DateTimeOffset Timestamp { get; set; }
    
    public double Weight { get; set; }
}