using CatScale.Service.DbModel;
using CatScale.Service.Model.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class AuthenticationController : ControllerBase
{
    private readonly ILogger<AuthenticationController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthenticationController(ILogger<AuthenticationController> logger, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _logger = logger;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user is null)
        {
            _logger.LogWarning("Login failed for user {UserName}, user does not exist", request.UserName);
            return BadRequest("Bad credentials");
        }
        
        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!signInResult.Succeeded)
        {
            _logger.LogWarning("Login failed for user {UserName}, invalid password", request.UserName);
            return BadRequest("Bad credentials");
        }

        await _signInManager.SignInAsync(user, request.RememberMe);

        _logger.LogWarning("Login successful for user {UserName} (RememberMe={RememberMe})", request.UserName, request.RememberMe);
        return Ok();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        _logger.LogWarning("Logout for user {Name}", User.Identity?.Name);
        await _signInManager.SignOutAsync();
        return Ok();
    }

    // TODO authorize ?
    [HttpGet]
    public ActionResult<UserInfo> UserInfo()
    {
        _logger.LogInformation("Returning UserInfo for user {Name}", User.Identity?.Name);
        
        var userInfo = new UserInfo
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
            UserName = User.Identity?.Name ?? String.Empty,
            ExposedClaims = User.Claims
                //Optionally: filter the claims you want to expose to the client
                //.Where(c => c.Type == "test-claim")
                .ToDictionary(c => c.Type, c => c.Value)
        };

        return Ok(userInfo);
    }

    [Authorize]
    [HttpGet]
    public ActionResult<string> Test()
    {
        return Ok("Hello!");
    }
}
