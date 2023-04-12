using CatScale.Service.Model.Cleaning;
using CatScale.Service.Model.Measurement;

namespace CatScale.Service.Model.Toilet;

public class ToiletDto
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string Description { get; set; } = null!;
    
    public MeasurementDto[] Measurements { get; set; } = null!;

    public CleaningDto[] Cleanings { get; set; } = null!;
}