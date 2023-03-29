using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.DbModel;

[Index(nameof(Value), IsUnique = true)]
public class UserApiKey
{
    public int Id { get; set; }

    public string Value { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;
}