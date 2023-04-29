using JetBrains.Annotations;

namespace CatScale.Service.Model.ScaleEvent;

[PublicAPI]
public record NewStablePhase
(
    DateTimeOffset Timestamp,
    double Length,
    double Value
);