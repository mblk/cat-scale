namespace CatScale.Service.RestModel;

public record NewCleaning(
    DateTimeOffset Timestamp,
    int ToiletId,
    double CleaningTime,
    double CleaningWeight);