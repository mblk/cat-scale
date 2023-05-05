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

[PublicAPI]
public record ScaleEventCounts
(
    int Total,
    int Cleanings,
    int Measurements
);

[PublicAPI]
public record ScaleEventStats
(
    ScaleEventCounts AllTime,
    ScaleEventCounts Yesterday,
    ScaleEventCounts Today
);

[PublicAPI]
public record PooCount
(
    int ToiletId,
    int Count
);