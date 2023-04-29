using JetBrains.Annotations;

namespace CatScale.Service.Model.ScaleEvent;

[PublicAPI]
public record StablePhaseDto
(
    int Id,
    DateTimeOffset Time,
    double Length,
    double Value
);