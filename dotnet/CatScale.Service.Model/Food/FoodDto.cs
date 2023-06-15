using JetBrains.Annotations;

namespace CatScale.Service.Model.Food;

[PublicAPI]
public record FoodDto
(
    int Id,
    string Brand,
    string Name,
    double CaloriesPerGram
);

[PublicAPI]
public record CreateFoodRequest
(
    string Brand,
    string Name,
    double CaloriesPerGram
);

[PublicAPI]
public record UpdateFoodRequest
(
    int Id,
    string Brand,
    string Name,
    double CaloriesPerGram
);

[PublicAPI]
public record FeedingDto
(
    int Id,
    int CatId,
    int FoodId,
    DateTimeOffset Timestamp,
    double Offered,
    double Eaten
);

[PublicAPI]
public record CreateFeedingRequest
(
    int CatId,
    int FoodId,
    DateTimeOffset Timestamp,
    double Offered,
    double Eaten
);
