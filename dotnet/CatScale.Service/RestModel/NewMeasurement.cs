namespace CatScale.Service.RestModel;

public record NewMeasurement(
    DateTimeOffset Timestamp,
    int ToiletId,
    double SetupTime,
    double PooTime,
    double CleanupTime,
    double CatWeight,
    double PooWeight);