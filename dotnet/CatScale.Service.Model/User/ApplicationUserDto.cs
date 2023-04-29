using JetBrains.Annotations;

namespace CatScale.Service.Model.User;

[PublicAPI]
public record ApplicationUserDto
(
    string UserName,
    string EMail,
    string[] Roles,
    bool IsAdmin
);