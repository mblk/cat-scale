using JetBrains.Annotations;

namespace CatScale.Service.Model.User;

[PublicAPI]
public class CreateUserResponse
{
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
}