using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace CatScale.Service.Model.Authentication;

[PublicAPI]
public class LoginRequest
{
    [Required(AllowEmptyStrings = false)]
    public string UserName { get; set; } = null!;
    
    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = null!;
    
    public bool RememberMe { get; set; }
}
