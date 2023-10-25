using JetBrains.Annotations;

namespace CatScale.Service.Model.ScaleEvent;

[PublicAPI]
public record ScaleEventStats
(
    ScaleEventCounts AllTime,
    ScaleEventCounts Yesterday,
    ScaleEventCounts Today
);

[PublicAPI]
public record ScaleEventCounts
(
    int Total,
    int Cleanings,
    int Measurements
);

[PublicAPI]
public record PooCount
(
    int ToiletId,
    int Count
);