namespace CatScale.Domain.Model;

public enum CatType
{
    Active,
    Inactive,
    Test,
}

public class Cat
{
    public int Id { get; set; }

    public CatType Type { get; set; }

    public string Name { get; set; } = null!;
    
    public DateOnly DateOfBirth { get; set; }

    public List<CatWeight> Weights { get; set; } = null!;
    
    public List<Measurement> Measurements { get; set; } = null!;

    public List<Feeding> Feedings { get; set; } = null!;
}
