using CatScale.Service.Model.Authentication;

namespace CatScale.UI.BlazorServer.Services.Authentication;

public interface IAuthenticationService
{
    event Action AuthenticationChanged;

    Task Login(string username, string password);
    
    Task Logout();
    
    Task<UserInfo?> GetUserInfo();

    Task<string?> Test();
}

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;

    public event Action? AuthenticationChanged;
    
    public AuthenticationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task Login(string username, string password)
    {
        var request = new LoginRequest()
        {
            UserName = username,
            Password = password,
            RememberMe = true,
        };
        
        var result = await _httpClient.PostAsJsonAsync("api/Authentication/Login", request);
        result.EnsureSuccessStatusCode();
        
        AuthenticationChanged?.Invoke();
    }

    public async Task Logout()
    {
        var result = await _httpClient.PostAsync("api/Authentication/Logout", null);
        result.EnsureSuccessStatusCode();
        
        AuthenticationChanged?.Invoke();
    }
    
    public async Task<UserInfo?> GetUserInfo()
    {
        return await _httpClient.GetFromJsonAsync<UserInfo>("api/Authentication/UserInfo");
    }

    public async Task<string?> Test()
    {
        return await _httpClient.GetStringAsync("api/Authentication/Test");   
    }
}
