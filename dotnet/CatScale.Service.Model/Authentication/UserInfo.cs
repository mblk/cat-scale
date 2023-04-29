using JetBrains.Annotations;

namespace CatScale.Service.Model.Authentication;

[PublicAPI]
public class UserInfo
{
    public bool IsAuthenticated { get; set; }

    public string UserName { get; set; } = null!;

    public UserClaim[] ExposedClaims { get; set; } = Array.Empty<UserClaim>();
}

[PublicAPI]
public record UserClaim
(
    string Type,
    string Value
);
