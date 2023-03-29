using System.ComponentModel.DataAnnotations;

namespace CatScale.Service.Model.Authentication;

public class LoginRequest
{
    [Required(AllowEmptyStrings = false)]
    public string UserName { get; set; } = null!;
    
    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = null!;
    
    public bool RememberMe { get; set; }
}
