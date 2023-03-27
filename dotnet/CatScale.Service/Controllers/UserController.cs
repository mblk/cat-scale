using CatScale.Service.RestModel;
using CatScale.Service.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IApiKeyService _apiKeyService;

    public UserController(ILogger<UserController> logger, UserManager<IdentityUser> userManager, IJwtService jwtService, IApiKeyService apiKeyService)
    {
        _logger = logger;
        _userManager = userManager;
        _jwtService = jwtService;
        _apiKeyService = apiKeyService;
    }

    // [HttpGet("{username}")]
    // public async Task<ActionResult<User>> GetUser(string username)
    // {
    //     IdentityUser? user = await _userManager.FindByNameAsync(username);
    //     if (user is null)
    //         return NotFound();
    //
    //     return new User
    //     {
    //         UserName = user.UserName,
    //         Email = user.Email
    //     };
    // }
    
    [HttpPost]
    public async Task<ActionResult<CreateUserResponse>> CreateUser(CreateUserRequest user)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _userManager.CreateAsync(new IdentityUser()
        {
            UserName = user.UserName,
            Email = user.Email,
        }, user.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var response = new CreateUserResponse()
        {
            UserName = user.UserName,
            Email = user.Email,
        };

        return CreatedAtAction(nameof(CreateUser), new { username = response.UserName }, response);
    }
    
    [HttpPost("BearerToken")]
    public async Task<ActionResult<AuthenticationResponse>> CreateBearerToken(AuthenticationRequest request)
    {
        _logger.LogInformation($"CreateBearerToken {ModelState.IsValid}");
        
        if (!ModelState.IsValid)
            return BadRequest("Bad credentials");

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user is null)
            return BadRequest("Bad credentials");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            return BadRequest("Bad credentials");

        var response = _jwtService.CreateToken(user);

        return Ok(response);
    }
    
    [HttpPost("ApiKey")]
    public async Task<ActionResult<AuthenticationResponse>> CreateApiKey(AuthenticationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user is null)
            return BadRequest("Bad credentials");

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
            return BadRequest("Bad credentials");

        var response = await _apiKeyService.CreateApiKey(user);

        return Ok(response);
    }
}
