using System.Data;
using CatScale.Service.Model.Cat;
using CatScale.Service.Model.ScaleEvent;
using CatScale.Service.Model.Toilet;
using CatScale.Service.Model.User;

namespace CatScale.UI.BlazorServer.Services;

public interface ICatScaleService // TODO split up
{
    Task<ToiletDto[]> GetToiletsList();
    Task<ToiletDto> GetToiletDetails(int id, ToiletDetails details);
    
    Task<CatDto[]> GetCatsList();
    Task<CatDto> GetCatDetails(int id, CatDetails details);

    Task<ScaleEventDto[]> GetScaleEvents();

    string GetScaleEventGraphUri(Uri sourceUri, int id);

    Task<string?> Test();

    Task<UserApiKeyDto[]> GetApiKeys();
    Task<UserApiKeyDto> CreateApiKey(DateTime? expirationDate);
    Task DeleteApiKey(int apiKeyId);

    Task<ApplicationUserDto> GetUserData();
    Task ChangeUserPassword(string oldPassword, string newPassword);
    Task DeleteUser(string password);

}

public class CatScaleService : ICatScaleService
{
    private readonly HttpClient _httpClient;

    public CatScaleService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<string?> Test()
    {
        return await _httpClient.GetStringAsync("api/Authentication/Test");   
    }

    public async Task<ToiletDto[]> GetToiletsList()
    {
        var response = await _httpClient.GetAsync("api/Toilet/GetAll");
        response.EnsureSuccessStatusCode();
    
        var toilets = await response.Content.ReadFromJsonAsync<ToiletDto[]>();
        if (toilets is null) throw new Exception("kaputt");
    
        return toilets;
    }
    
    public async Task<ToiletDto> GetToiletDetails(int id, ToiletDetails details)
    {
        var response = await _httpClient.GetAsync($"api/Toilet/GetOne/{id}?details={details}");
        response.EnsureSuccessStatusCode();
        
        var toilet = await response.Content.ReadFromJsonAsync<ToiletDto>();
        if (toilet is null) throw new Exception("kaputt");
    
        return toilet;
    }
    
    public async Task<CatDto[]> GetCatsList()
    {
        var response = await _httpClient.GetAsync("api/Cat/GetAll");
        response.EnsureSuccessStatusCode();
    
        var cats = await response.Content.ReadFromJsonAsync<CatDto[]>();
        if (cats is null) throw new Exception("kaputt");
    
        return cats;
    }
    
    public async Task<CatDto> GetCatDetails(int id, CatDetails details)
    {
        var response = await _httpClient.GetAsync($"api/Cat/GetOne/{id}?details={details}");
        response.EnsureSuccessStatusCode();
        
        var cat = await response.Content.ReadFromJsonAsync<CatDto>();
        if (cat is null) throw new Exception("kaputt");
    
        return cat;
    }

    public async Task<ScaleEventDto[]> GetScaleEvents()
    {
        var response = await _httpClient.GetAsync($"api/ScaleEvent/GetAll");
        response.EnsureSuccessStatusCode();

        var scaleEvents = await response.Content.ReadFromJsonAsync<ScaleEventDto[]>();
        if (scaleEvents is null) throw new Exception("kaputt");

        return scaleEvents;
    }

    public string GetScaleEventGraphUri(Uri sourceUri, int id)
    {
        Console.WriteLine($"GetScaleEventGraphUri source={sourceUri} base={_httpClient.BaseAddress} id={id}");

        var host = sourceUri.Host;
        var port = _httpClient.BaseAddress?.Port ?? 5000;
        var scheme = _httpClient.BaseAddress?.Scheme ?? "http";

        var s = $"{scheme}://{host}:{port}/api/Graph/GetScaleEvent?scaleEventId={id}";
        return s;
    }
    
    public async Task<UserApiKeyDto[]> GetApiKeys()
    {
        var response = await _httpClient.GetAsync("api/User/GetApiKeys");
        response.EnsureSuccessStatusCode();
    
        var apiKeys = await response.Content.ReadFromJsonAsync<UserApiKeyDto[]>();
        if (apiKeys is null) throw new Exception("kaputt");
    
        return apiKeys;
    }

    public async Task<UserApiKeyDto> CreateApiKey(DateTime? expirationDate)
    {
        var request = new CreateApiKeyRequest(expirationDate);

        var response = await _httpClient.PostAsJsonAsync("api/User/CreateApiKey", request);
        response.EnsureSuccessStatusCode();

        var apiKey = await response.Content.ReadFromJsonAsync<UserApiKeyDto>();
        if (apiKey is null) throw new Exception("kaputt");

        return apiKey;
    }

    public async Task DeleteApiKey(int apiKeyId)
    {
        var response = await _httpClient.DeleteAsync($"api/User/DeleteApiKey/{apiKeyId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<ApplicationUserDto> GetUserData()
    {
        var userData = await _httpClient.GetFromJsonAsync<ApplicationUserDto>("api/User/Get");
        if (userData is null) throw new Exception("kaputt");
        return userData;
    }

    public async Task ChangeUserPassword(string oldPassword, string newPassword)
    {
        var request = new ChangePasswordRequest(oldPassword, newPassword);
        var response = await _httpClient.PostAsJsonAsync("api/User/ChangePassword", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUser(string password)
    {
        var request = new DeleteUserRequest(password);

        var response = await _httpClient.PostAsJsonAsync("api/User/DeleteUser", request);
        response.EnsureSuccessStatusCode();
    }
}
