namespace CatScale.Service.Model.User;

public record CreateApiKeyRequest
(
    DateTime? ExpirationDate
);