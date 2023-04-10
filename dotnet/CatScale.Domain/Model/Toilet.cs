namespace CatScale.Domain.Model;

public class Toilet
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string Description { get; set; } = null!;
    
    public List<ScaleEvent> ScaleEvents { get; set; } = null!;
}