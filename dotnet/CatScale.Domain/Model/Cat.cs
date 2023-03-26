namespace CatScale.Domain.Model;

public class Cat
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public DateOnly DateOfBirth { get; set; }

    public List<CatWeight> Weights { get; set; } = null!;
    
    public List<Measurement> Measurements { get; set; } = null!;
}