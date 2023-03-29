using System.Security.Claims;
using CatScale.Service.Model.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace CatScale.UI.BlazorServer.Services.Authentication;

public class IdentityAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly IAuthenticationService _authenticationService;
    
    //private UserInfo? _userInfoCache;

    public IdentityAuthenticationStateProvider(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
        _authenticationService.AuthenticationChanged += AuthenticationServiceOnAuthenticationChanged;
    }

    private void AuthenticationServiceOnAuthenticationChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());    
    }

    private async Task<UserInfo?> GetUserInfo()
    {
        // if (_userInfoCache != null && _userInfoCache.IsAuthenticated)
        //     return _userInfoCache;
        //
        // _userInfoCache = await _authenticationService.GetUserInfo();
        //
        // return _userInfoCache;
        
        return await _authenticationService.GetUserInfo();
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        
        var userInfo = await GetUserInfo();
        if (userInfo != null && userInfo.IsAuthenticated)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, userInfo.UserName) }
                .Concat(userInfo.ExposedClaims.Select(c => new Claim(c.Key, c.Value)));
            identity = new ClaimsIdentity(claims, "Server authentication");
        }

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
