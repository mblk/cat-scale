using JetBrains.Annotations;

namespace CatScale.Service.Model.Toilet;

[PublicAPI]
public class ToiletDto
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string Description { get; set; } = null!;
}