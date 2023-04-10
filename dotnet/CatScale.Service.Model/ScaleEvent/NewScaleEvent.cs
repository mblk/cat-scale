namespace CatScale.Service.Model.ScaleEvent;

public record NewScaleEvent
(
    int ToiletId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    NewStablePhase[] StablePhases
);