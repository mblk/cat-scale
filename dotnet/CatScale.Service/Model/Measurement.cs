namespace CatScale.Service.Model;

public record Measurement(DateTimeOffset TimeStamp,
    double SetupTime, double PooTime, double CleanupTime,
    double CatWeight, double PooWeight);