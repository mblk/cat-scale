using System.Text.Json;
using CatScale.Service.Model.Cat;
using CatScale.Service.Model.Measurement;
using CatScale.Service.Model.ScaleEvent;
using CatScale.Service.Model.Toilet;
using CatScale.Service.Model.User;

namespace CatScale.UI.BlazorServer.Services;

public interface ICatScaleService // TODO split up
{
    //
    // Toilet
    //

    Task<ToiletDto[]> GetToiletsList();
    Task<ToiletDto> GetToiletDetails(int id);

    //
    // Cat
    //

    Task<CatDto[]> GetAllCats();
    Task<CatDto> GetCat(int id);
    Task<CatDto> CreateCat(string name, DateOnly dateOfBirth);
    Task<CatDto> UpdateCat(int id, string name, DateOnly dateOfBirth);
    Task DeleteCat(int id);

    Task<CatWeightDto[]> GetCatWeights(int catId);
    Task<CatWeightDto> CreateCatWeight(int catId, DateTimeOffset timestamp, double weight);
    Task DeleteCatWeight(int catWeightId);

    Task<MeasurementDto[]> GetCatMeasurements(int catId);

    //
    // ScaleEvents
    //

    Task<ScaleEventDto[]> GetScaleEvents();
    Task DeleteScaleEvent(int id);
    Task ClassifyScaleEvent(int id);
    Task ClassifyAllScaleEvents();
    Task DeleteAllScaleEventClassifications();

    Task<ScaleEventStats> GetScaleEventStats();
    Task<PooCount[]> GetPooCounts();

    //
    // Graphs
    //
    string GetScaleEventGraphUri(Uri sourceUri, int id);
    string GetCatGraphUri(Uri sourceUri, int id);

    //
    // ApiKeys
    //

    Task<UserApiKeyDto[]> GetApiKeys();
    Task<UserApiKeyDto> CreateApiKey(DateTime? expirationDate);
    Task DeleteApiKey(int apiKeyId);

    //
    // User
    //

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

    //
    // Toilet
    //

    public async Task<ToiletDto[]> GetToiletsList()
        => await _httpClient.GetFromJsonAsync<ToiletDto[]>("api/Toilet/GetAll") ??
           throw new JsonException("Failed to deserialize response");


    public async Task<ToiletDto> GetToiletDetails(int id)
        => await _httpClient.GetFromJsonAsync<ToiletDto>($"api/Toilet/GetOne/{id}") ??
           throw new JsonException("Failed to deserialize response");

    //
    // Cat
    //

    public async Task<CatDto[]> GetAllCats()
        => await _httpClient.GetFromJsonAsync<CatDto[]>("api/Cat/GetAll") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<CatDto> GetCat(int id)
        => await _httpClient.GetFromJsonAsync<CatDto>($"api/Cat/GetOne/{id}") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<CatDto> CreateCat(string name, DateOnly dateOfBirth)
    {
        var response = (await _httpClient.PutAsJsonAsync("api/Cat/Create",
                new CreateCatRequest(name, dateOfBirth)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatDto>() ??
               throw new JsonException("Failed to deserialize response");
    }

    public async Task<CatDto> UpdateCat(int id, string name, DateOnly dateOfBirth)
    {
        var response = (await _httpClient.PostAsJsonAsync($"api/Cat/Update/{id}",
                new UpdateCatRequest(name, dateOfBirth)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatDto>() ??
               throw new JsonException("Failed to deserialize response");
    }

    public async Task DeleteCat(int id)
        => (await _httpClient.DeleteAsync($"api/Cat/Delete/{id}"))
            .EnsureSuccessStatusCode();

    public async Task<CatWeightDto[]> GetCatWeights(int catId)
        => await _httpClient.GetFromJsonAsync<CatWeightDto[]>($"api/CatWeight/GetAll/{catId}") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<CatWeightDto> CreateCatWeight(int catId, DateTimeOffset timestamp, double weight)
    {
        var response = (await _httpClient.PostAsJsonAsync("api/CatWeight/Create",
                new CreateCatWeightRequest(catId, timestamp, weight)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatWeightDto>() ??
               throw new JsonException("Failed to deserialize response");
    }

    public async Task DeleteCatWeight(int catWeightId)
        => (await _httpClient.DeleteAsync($"api/CatWeight/Delete/{catWeightId}"))
            .EnsureSuccessStatusCode();

    public async Task<MeasurementDto[]> GetCatMeasurements(int catId)
        => await _httpClient.GetFromJsonAsync<MeasurementDto[]>($"api/Measurement/GetAll/{catId}") ??
           throw new JsonException("Failed to deserialize response");

    //
    // ScaleEvents
    //

    public async Task<ScaleEventDto[]> GetScaleEvents()
        => await _httpClient.GetFromJsonAsync<ScaleEventDto[]>($"api/ScaleEvent/GetAll") ??
           throw new JsonException("Failed to deserialize response");

    public async Task DeleteScaleEvent(int id)
        => (await _httpClient.DeleteAsync($"api/ScaleEvent/Delete/{id}"))
            .EnsureSuccessStatusCode();

    public async Task ClassifyScaleEvent(int id)
        => (await _httpClient.PostAsync($"api/ScaleEvent/Classify/{id}", null))
            .EnsureSuccessStatusCode();

    public async Task ClassifyAllScaleEvents()
        => (await _httpClient.PostAsync($"api/ScaleEvent/ClassifyAllEvents", null))
            .EnsureSuccessStatusCode();

    public async Task DeleteAllScaleEventClassifications()
        => (await _httpClient.PostAsync($"api/ScaleEvent/DeleteAllClassifications", null))
            .EnsureSuccessStatusCode();

    public async Task<ScaleEventStats> GetScaleEventStats()
        => await _httpClient.GetFromJsonAsync<ScaleEventStats>("api/ScaleEvent/GetStats") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<PooCount[]> GetPooCounts()
        => await _httpClient.GetFromJsonAsync<PooCount[]>("api/ScaleEvent/GetPooCounts") ??
           throw new JsonException("Failed to deserialize response");

    //
    // Graphs
    //

    public string GetScaleEventGraphUri(Uri sourceUri, int id)
    {
        var host = sourceUri.Host;
        var port = _httpClient.BaseAddress?.Port ?? 5000;
        var scheme = _httpClient.BaseAddress?.Scheme ?? "http";

        var s = $"{scheme}://{host}:{port}/api/Graph/GetScaleEvent?scaleEventId={id}";
        return s;
    }

    public string GetCatGraphUri(Uri sourceUri, int id)
    {
        var host = sourceUri.Host;
        var port = _httpClient.BaseAddress?.Port ?? 5000;
        var scheme = _httpClient.BaseAddress?.Scheme ?? "http";

        var s = $"{scheme}://{host}:{port}/api/Graph/GetCatMeasurements?catId={id}";
        return s;
    }

    //
    // ApiKeys
    //

    public async Task<UserApiKeyDto[]> GetApiKeys()
        => await _httpClient.GetFromJsonAsync<UserApiKeyDto[]>("api/User/GetApiKeys") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<UserApiKeyDto> CreateApiKey(DateTime? expirationDate)
    {
        var response = (await _httpClient.PostAsJsonAsync("api/User/CreateApiKey",
                new CreateApiKeyRequest(expirationDate)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UserApiKeyDto>() ??
               throw new JsonException("Failed to deserialize response");
    }

    public async Task DeleteApiKey(int apiKeyId)
        => (await _httpClient.DeleteAsync($"api/User/DeleteApiKey/{apiKeyId}"))
            .EnsureSuccessStatusCode();

    //
    // User
    //

    public async Task<ApplicationUserDto> GetUserData()
        => await _httpClient.GetFromJsonAsync<ApplicationUserDto>("api/User/Get") ??
           throw new JsonException("Failed to deserialize response");

    public async Task ChangeUserPassword(string oldPassword, string newPassword)
        => (await _httpClient.PostAsJsonAsync("api/User/ChangePassword",
                new ChangePasswordRequest(oldPassword, newPassword)))
            .EnsureSuccessStatusCode();

    public async Task DeleteUser(string password)
        => (await _httpClient.PostAsJsonAsync("api/User/DeleteUser", new DeleteUserRequest(password)))
            .EnsureSuccessStatusCode();
}