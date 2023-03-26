namespace CatScale.Domain.Model;

public class Toilet
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string Description { get; set; } = null!;
    
    public List<Measurement> Measurements { get; set; } = null!;
    
    public List<Cleaning> Cleanings { get; set; } = null!;
}