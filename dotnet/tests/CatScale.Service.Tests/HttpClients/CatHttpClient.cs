using CatScale.Service.Model.Cat;

namespace CatScale.Service.Tests.HttpClients;

public class CatHttpClient
{
    private readonly HttpClient _client;

    public CatHttpClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<CatDto[]> GetAll()
        => await _client.GetFromJsonAsync<CatDto[]>("api/Cat/GetAll")
           ?? throw new Exception("Failed to deserialize response");

    public async Task<CatDto> Get(int id)
        => await _client.GetFromJsonAsync<CatDto>($"api/Cat/GetOne/{id}") ??
           throw new Exception("Failed to deserialize response");

    public async Task<CatDto> Create(CatTypeDto type, string name, DateOnly dateOfBirth)
    {
        var request = new CreateCatRequest(type, name, dateOfBirth);
        var response = (await _client.PutAsJsonAsync("api/Cat/Create", request))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatDto>() ??
               throw new Exception("Failed to deserialize response");
    }

    public async Task<CatDto> Update(int id, CatTypeDto type, string name, DateOnly dateOfBirth)
    {
        var response = (await _client.PostAsJsonAsync($"api/Cat/Update/{id}",
                new UpdateCatRequest(type, name, dateOfBirth)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatDto>() ??
               throw new Exception("Failed to deserialize response");
    }

    public async Task Delete(int id)
        => (await _client.DeleteAsync($"api/Cat/Delete/{id}"))
            .EnsureSuccessStatusCode();
}