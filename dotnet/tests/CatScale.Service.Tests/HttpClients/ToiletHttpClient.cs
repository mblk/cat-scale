using CatScale.Service.Model.Toilet;

namespace CatScale.Service.Tests.HttpClients;

public class ToiletHttpClient
{
    private readonly HttpClient _client;

    public ToiletHttpClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<ToiletDto[]> GetAll()
        => await _client.GetFromJsonAsync<ToiletDto[]>("api/Toilet/GetAll")
           ?? throw new Exception("Failed to deserialize response");

    public async Task<ToiletDto> Get(int id)
        => await _client.GetFromJsonAsync<ToiletDto>($"api/Toilet/GetOne/{id}") ??
           throw new Exception("Failed to deserialize response");

    public async Task<ToiletDto> Create(string name, string description)
    {
        var request = new CreateToiletRequest(name, description);
        var response = (await _client.PutAsJsonAsync("api/Toilet/Create", request))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ToiletDto>() ??
               throw new Exception("Failed to deserialize response");
    }

    public async Task<ToiletDto> Update(int id, string name, string description)
    {
        var response = (await _client.PostAsJsonAsync($"api/Toilet/Update/{id}",
                new UpdateToiletRequest(name, description)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ToiletDto>() ??
               throw new Exception("Failed to deserialize response");
    }

    public async Task Delete(int id)
        => (await _client.DeleteAsync($"api/Toilet/Delete/{id}"))
            .EnsureSuccessStatusCode();
}