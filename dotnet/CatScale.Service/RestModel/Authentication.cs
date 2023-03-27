using System.ComponentModel.DataAnnotations;

namespace CatScale.Service.RestModel;

public class AuthenticationRequest
{
    [Required(AllowEmptyStrings = false)]
    public string UserName { get; set; } = null!;
    
    [Required(AllowEmptyStrings = false)]
    public string Password { get; set; } = null!;
}

public class AuthenticationResponse
{
    public string Token { get; set; } = null!;
    public DateTime Expiration { get; set; }
}
