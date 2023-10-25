using CatScale.Service.Model.Cat;

namespace CatScale.Service.Tests.HttpClients;

public class CatWeightHttpClient
{
    private readonly HttpClient _client;

    public CatWeightHttpClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<CatWeightDto[]> GetAll(int catId)
        => await _client.GetFromJsonAsync<CatWeightDto[]>($"api/CatWeight/GetAll/{catId}")
           ?? throw new Exception("Failed to deserialize response");

    public async Task<CatWeightDto> Get(int id)
        => await _client.GetFromJsonAsync<CatWeightDto>($"api/CatWeight/GetOne/{id}")
           ?? throw new Exception("Failed to deserialize response");

    public async Task<CatWeightDto> Create(int catId, DateTimeOffset timestamp, double weight)
    {
        var response = (await _client.PostAsJsonAsync("api/CatWeight/Create",
                new CreateCatWeightRequest(catId, timestamp, weight)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatWeightDto>() ??
               throw new Exception("Failed to deserialize response");
    }

    public async Task Delete(int catWeightId)
        => (await _client.DeleteAsync($"api/CatWeight/Delete/{catWeightId}"))
            .EnsureSuccessStatusCode();
}