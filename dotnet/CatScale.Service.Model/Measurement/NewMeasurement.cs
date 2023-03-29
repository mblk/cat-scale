namespace CatScale.Service.Model.Measurement;

public record NewMeasurement(
    DateTimeOffset Timestamp,
    int ToiletId,
    double SetupTime,
    double PooTime,
    double CleanupTime,
    double CatWeight,
    double PooWeight);