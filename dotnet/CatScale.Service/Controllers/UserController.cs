using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.User;
using CatScale.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApiKeyService _apiKeyService;

    private readonly DataMapper _mapper = new();
    
    public UserController(ILogger<UserController> logger, UserManager<ApplicationUser> userManager, IApiKeyService apiKeyService)
    {
        _logger = logger;
        _userManager = userManager;
        _apiKeyService = apiKeyService;
    }

    private async Task<ApplicationUser?> TryGetAuthorizedUser()
    {
        var currentUserName = User.Identity?.Name;
        if (String.IsNullOrWhiteSpace(currentUserName))
            return null;

        return await _userManager.FindByNameAsync(currentUserName);
    }
    
    [HttpPost]
    public async Task<ActionResult<CreateUserResponse>> CreateUser(CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
    
        var result = await _userManager.CreateAsync(new ApplicationUser()
        {
            UserName = request.UserName,
            Email = request.Email,
        }, request.Password);
    
        if (!result.Succeeded)
            return BadRequest(result.Errors);
    
        var response = new CreateUserResponse()
        {
            UserName = request.UserName,
            Email = request.Email,
        };
        
        _logger.LogInformation("Created new user {UserName}", request.UserName);
        return CreatedAtAction(nameof(CreateUser), new { username = response.UserName }, response);
    }
    
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<UserApiKeyDto[]>> GetApiKeys()
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var user = await TryGetAuthorizedUser();
        if (user is null)
            return BadRequest("Invalid user");

        var apiKeys = (await _apiKeyService.GetApiKeys(user))
            .Select(_mapper.MapUserApiKey);

        return Ok(apiKeys);
    }
    
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<UserApiKeyDto>> CreateApiKey(CreateApiKeyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var user = await TryGetAuthorizedUser();
        if (user is null)
            return BadRequest("Invalid user");
        
        var apiKey = await _apiKeyService.CreateApiKey(user, request.ExpirationDate);
        var apiKeyDto = _mapper.MapUserApiKey(apiKey);

        return CreatedAtAction(nameof(CreateApiKey), new { Id = apiKeyDto.Id }, apiKeyDto);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteApiKey(int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await TryGetAuthorizedUser();
        if (user is null)
            return BadRequest("Invalid user");
        
        await _apiKeyService.DeleteApiKey(user, id);

        return Ok();
    }
    
    // [HttpPost("BearerToken")]
    // public async Task<ActionResult<AuthenticationResponse>> CreateBearerToken(AuthenticationRequest request)
    // {
    //     _logger.LogInformation($"CreateBearerToken {ModelState.IsValid}");
    //     
    //     if (!ModelState.IsValid)
    //         return BadRequest("Bad credentials");
    //
    //     var user = await _userManager.FindByNameAsync(request.UserName);
    //     if (user is null)
    //         return BadRequest("Bad credentials");
    //
    //     var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
    //     if (!isPasswordValid)
    //         return BadRequest("Bad credentials");
    //
    //     var response = _jwtService.CreateToken(user);
    //
    //     return Ok(response);
    // }
}
