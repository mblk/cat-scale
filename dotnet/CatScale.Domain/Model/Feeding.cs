namespace CatScale.Domain.Model;

public class Feeding
{
    public int Id { get; set; }
    
    public int CatId { get; set; }
    
    public int FoodId { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Amount of food in grams offered to the cat.
    /// </summary>
    public double Offered { get; set; }
    
    /// <summary>
    /// Amount of food in grams eaten by the cat.
    /// Value is 0 if the cat did not like the food.
    /// </summary>
    public double Eaten { get; set; }
}
