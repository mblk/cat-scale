using Microsoft.AspNetCore.Identity;

namespace CatScale.Service.DbModel;

public static class ApplicationRoles
{
    public const string Admin = "Admin";
}

public class ApplicationRole : IdentityRole<Guid>
{
}