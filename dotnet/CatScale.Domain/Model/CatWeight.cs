namespace CatScale.Domain.Model;

public class CatWeight
{
    public int Id { get; set; }
    
    private int CatId { get; set; }
    //public Cat Cat { get; set; } = null!;
    
    public DateTimeOffset Timestamp { get; set; }
    
    public double Weight { get; set; }
}