namespace CatScale.Service.Model.Authentication;

public class UserInfo
{
    public bool IsAuthenticated { get; set; }

    public string UserName { get; set; } = null!;

    public UserClaim[] ExposedClaims { get; set; } = Array.Empty<UserClaim>();
}

public record UserClaim
(
    string Type,
    string Value
);
