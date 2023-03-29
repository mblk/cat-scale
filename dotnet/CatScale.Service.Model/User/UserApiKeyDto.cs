namespace CatScale.Service.Model.User;

public record UserApiKeyDto
(
    int Id,
    string Value,
    DateTime ExpirationDate
);
