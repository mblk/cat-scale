using JetBrains.Annotations;

namespace CatScale.Service.Model.Cat;

[PublicAPI]
public record CatWeightDto
(
    int Id,
    DateTimeOffset Timestamp,
    double Weight
);