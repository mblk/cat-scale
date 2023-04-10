namespace CatScale.Service.Model.ScaleEvent;

public record StablePhaseDto
(
    int Id,
    DateTimeOffset Time,
    double Length,
    double Value
);