namespace CatScale.Service.Model.Authentication;

public class UserInfo
{
    public bool IsAuthenticated { get; set; }

    public string UserName { get; set; } = null!;

    public Dictionary<string, string> ExposedClaims { get; set; } = null!;
}
