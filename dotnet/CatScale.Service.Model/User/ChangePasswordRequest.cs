using JetBrains.Annotations;

namespace CatScale.Service.Model.User;

[PublicAPI]
public record ChangePasswordRequest
(
    string OldPassword,
    string NewPassword
);