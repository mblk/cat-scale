using JetBrains.Annotations;

namespace CatScale.Service.Model.User;

[PublicAPI]
public record CreateApiKeyRequest
(
    DateTime? ExpirationDate
);