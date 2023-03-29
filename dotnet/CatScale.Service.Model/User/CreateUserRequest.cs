using System.ComponentModel.DataAnnotations;

namespace CatScale.Service.Model.User;

public class CreateUserRequest
{
    [Required(AllowEmptyStrings = false)]
    public string UserName { get; set; } = null!;
    
    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string PasswordConfirm { get; set; } = null!;
    
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    public string Email { get; set; } = null!;
}