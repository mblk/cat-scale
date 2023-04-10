namespace CatScale.Service.Model.ScaleEvent;

public record NewStablePhase
(
    DateTimeOffset Timestamp,
    double Length,
    double Value
);