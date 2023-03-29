namespace CatScale.Service.Model.Cleaning;

public record NewCleaning(
    DateTimeOffset Timestamp,
    int ToiletId,
    double CleaningTime,
    double CleaningWeight);
