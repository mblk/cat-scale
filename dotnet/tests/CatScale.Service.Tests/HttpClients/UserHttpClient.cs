using CatScale.Service.Model.User;

namespace CatScale.Service.Tests.HttpClients;

public class UserHttpClient
{
    private readonly HttpClient _client;

    public UserHttpClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<UserApiKeyDto[]> GetApiKeys()
        => await _client.GetFromJsonAsync<UserApiKeyDto[]>("api/User/GetApiKeys") ??
           throw new Exception("Failed to deserialize response");

    public async Task<UserApiKeyDto> CreateApiKey(DateTime? expirationDate)
    {
        var response = (await _client.PostAsJsonAsync("api/User/CreateApiKey",
                new CreateApiKeyRequest(expirationDate)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserApiKeyDto>() ??
               throw new Exception("Failed to deserialize response");
    }

    public async Task DeleteApiKey(int apiKeyId)
        => (await _client.DeleteAsync($"api/User/DeleteApiKey/{apiKeyId}"))
            .EnsureSuccessStatusCode();
}