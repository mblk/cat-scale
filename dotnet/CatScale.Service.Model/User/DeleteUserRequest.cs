using JetBrains.Annotations;

namespace CatScale.Service.Model.User;

[PublicAPI]
public record DeleteUserRequest
(
    string Password
);