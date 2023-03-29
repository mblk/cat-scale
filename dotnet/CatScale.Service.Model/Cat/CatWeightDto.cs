namespace CatScale.Service.Model.Cat;

public record CatWeightDto(
    int Id,
    DateTimeOffset Timestamp,
    double Weight
    );