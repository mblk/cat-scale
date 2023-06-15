namespace CatScale.Domain.Model;

public enum FoodType
{
    Dry,
    Wet,
    Treat,
}

public class Food
{
    public int Id { get; set; }
    
    public string Brand { get; set; } = null!;

    public string Name { get; set; } = null!;
    
    public FoodType Type { get; set; }
    
    public double CaloriesPerGram { get; set; }
    
    // TODO list ingredients / composition ?
    
    public List<Feeding> Feedings { get; set; } = null!;
}