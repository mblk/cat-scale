using JetBrains.Annotations;

namespace CatScale.Service.Model.User;

[PublicAPI]
public record UserApiKeyDto
(
    int Id,
    string Value,
    DateTime ExpirationDate
);
