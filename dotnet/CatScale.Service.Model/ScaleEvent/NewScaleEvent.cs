using JetBrains.Annotations;

namespace CatScale.Service.Model.ScaleEvent;

[PublicAPI]
public record NewScaleEvent
(
    int ToiletId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    NewStablePhase[] StablePhases,
    double Temperature,
    double Humidity,
    double Pressure
);