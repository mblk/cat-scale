using JetBrains.Annotations;

namespace CatScale.Service.Model.Cat;

[PublicAPI]
public record CatWeightDto
(
    int Id,
    DateTimeOffset Timestamp,
    double Weight
);

[PublicAPI]
public record CreateCatWeightRequest
(
    int CatId,
    DateTimeOffset Timestamp,
    double Weight
);
