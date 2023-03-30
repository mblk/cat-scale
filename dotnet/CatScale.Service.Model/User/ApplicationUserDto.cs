namespace CatScale.Service.Model.User;

public record ApplicationUserDto
(
    string UserName,
    string EMail,
    string[] Roles,
    bool IsAdmin
);