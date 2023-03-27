using System.ComponentModel.DataAnnotations;

namespace CatScale.Service.RestModel;

public class CreateUserRequest
{
    [Required(AllowEmptyStrings = false)]
    public string UserName { get; set; } = null!;
    
    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = null!;
    
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string Email { get; set; } = null!;
}

public class CreateUserResponse
{
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
}
