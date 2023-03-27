using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.DbModel;

[Index(nameof(Value), IsUnique = true)]
public class UserApiKey
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public IdentityUser User { get; set; } = null!;
}
