namespace CatScale.Service.Model.User;

public record ChangePasswordRequest
(
    string OldPassword,
    string NewPassword
);