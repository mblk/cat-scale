using CatScale.Service.Model.Measurement;

namespace CatScale.Service.Model.Cat;

public class CatDto
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public DateOnly DateOfBirth { get; set; }

    public CatWeightDto[] Weights { get; set; } = Array.Empty<CatWeightDto>();

    public MeasurementDto[] Measurements { get; set; } = Array.Empty<MeasurementDto>();
}