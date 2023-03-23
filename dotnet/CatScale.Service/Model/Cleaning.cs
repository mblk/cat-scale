namespace CatScale.Service.Model;

public record Cleaning(DateTimeOffset TimeStamp,
    double CleaningTime, double CleaningWeight);