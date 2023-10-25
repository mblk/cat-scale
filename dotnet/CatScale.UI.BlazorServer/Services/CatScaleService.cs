using System.Text;
using System.Text.Json;
using CatScale.Service.Model.Cat;
using CatScale.Service.Model.Food;
using CatScale.Service.Model.Measurement;
using CatScale.Service.Model.ScaleEvent;
using CatScale.Service.Model.Toilet;
using CatScale.Service.Model.User;

namespace CatScale.UI.BlazorServer.Services;

public interface ICatScaleService // TODO split up?
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
    Task<CatDto> CreateCat(CatTypeDto type, string name, DateOnly dateOfBirth);
    Task<CatDto> UpdateCat(int id, CatTypeDto type, string name, DateOnly dateOfBirth);
    Task DeleteCat(int id);

    Task<CatWeightDto[]> GetCatWeights(int catId);
    Task<CatWeightDto> CreateCatWeight(int catId, DateTimeOffset timestamp, double weight);
    Task DeleteCatWeight(int catWeightId);

    Task<MeasurementDto[]> GetCatMeasurements(int catId);

    //
    // ScaleEvents
    //

    Task<(ScaleEventDto[], int)> GetScaleEvents(int? toiletId = null, int? skip = null, int? take = null);
    Task DeleteScaleEvent(int id);
    Task ClassifyScaleEvent(int id);

    Task<ScaleEventStats> GetScaleEventStats();
    Task<PooCount[]> GetPooCounts();

    //
    // Food
    //

    Task<FoodDto[]> GetAllFoods();
    Task<FoodDto> GetOneFood(int id);
    Task<FoodDto> CreateFood(string brand, string name, double caloriesPerGram);
    Task<FoodDto> UpdateFood(int id, string brand, string name, double caloriesPerGram);
    Task DeleteFood(int id);

    //
    // Feeding
    //

    Task<FeedingDto[]> GetAllFeedings();
    Task<FeedingDto> GetOneFeeding(int id);
    Task<FeedingDto> CreateFeeding(int catId, int foodId, DateTimeOffset timestamp, double offered, double eaten);
    Task DeleteFeeding(int id);

    //
    // Graphs
    //

    string GetScaleEventGraphUri(int id);
    string GetCatGraphUri(int id, DateTimeOffset? minTime, DateTimeOffset? maxTime, bool includeTemperature);
    string GetCombinedCatGraphUri(int id1, int id2, bool sameAxis, DateTimeOffset? minTime, DateTimeOffset? maxTime);
    string GetToiletGraphUri(int id, ToiletSensorValue sensorValue);
    string GetCombinedToiletGraphUri(int id, ToiletSensorValue sensorValue1, ToiletSensorValue sensorValue2);

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
    private readonly IWebHostEnvironment _environment;

    public CatScaleService(HttpClient httpClient, IWebHostEnvironment environment)
    {
        _httpClient = httpClient;
        _environment = environment;
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

    public async Task<CatDto> CreateCat(CatTypeDto type, string name, DateOnly dateOfBirth)
    {
        var response = (await _httpClient.PutAsJsonAsync("api/Cat/Create",
                new CreateCatRequest(type, name, dateOfBirth)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CatDto>() ??
               throw new JsonException("Failed to deserialize response");
    }

    public async Task<CatDto> UpdateCat(int id, CatTypeDto type, string name, DateOnly dateOfBirth)
    {
        var response = (await _httpClient.PostAsJsonAsync($"api/Cat/Update/{id}",
                new UpdateCatRequest(type, name, dateOfBirth)))
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

    public async Task<(ScaleEventDto[], int)> GetScaleEvents(int? toiletId = null, int? skip = null, int? take = null)
    {
        var parameters = new List<string>();

        if (toiletId != null)
            parameters.Add($"toiletId={toiletId}");
        if (skip != null)
            parameters.Add($"skip={skip}");
        if (take != null)
            parameters.Add($"take={take}");

        var uri = "api/ScaleEvent/GetAll";

        if (parameters.Any())
        {
            uri += "?";
            uri += String.Join("&", parameters);
        }

        var response = (await _httpClient.GetAsync(uri))
            .EnsureSuccessStatusCode();
        
        int totalCount = 0;
        if (response.Headers.TryGetValues("X-Total-Count", out var totalCountValues))
        {
            var s = totalCountValues.FirstOrDefault() ?? String.Empty;
            _ = Int32.TryParse(s, out totalCount);
        }

        var scaleEvents = await response.Content.ReadFromJsonAsync<ScaleEventDto[]>()
               ?? throw new JsonException("Failed to deserialize response");

        return (scaleEvents, totalCount);
    }

    public async Task DeleteScaleEvent(int id)
        => (await _httpClient.DeleteAsync($"api/ScaleEvent/Delete/{id}"))
            .EnsureSuccessStatusCode();

    public async Task ClassifyScaleEvent(int id)
        => (await _httpClient.PostAsync($"api/ScaleEvent/Classify/{id}", null))
            .EnsureSuccessStatusCode();

    public async Task<ScaleEventStats> GetScaleEventStats()
        => await _httpClient.GetFromJsonAsync<ScaleEventStats>("api/ScaleEvent/GetStats") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<PooCount[]> GetPooCounts()
        => await _httpClient.GetFromJsonAsync<PooCount[]>("api/ScaleEvent/GetPooCounts") ??
           throw new JsonException("Failed to deserialize response");

    //
    // Food
    //

    public async Task<FoodDto[]> GetAllFoods()
        => await _httpClient.GetFromJsonAsync<FoodDto[]>($"api/Food/GetAll") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<FoodDto> GetOneFood(int id)
        => await _httpClient.GetFromJsonAsync<FoodDto>($"api/Food/GetOne/{id}") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<FoodDto> CreateFood(string brand, string name, double caloriesPerGram)
    {
        var response = (await _httpClient.PutAsJsonAsync("api/Food/Create",
                new CreateFoodRequest(brand, name, caloriesPerGram)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FoodDto>() ??
               throw new JsonException("Failed to deserialize response");
    }

    public async Task<FoodDto> UpdateFood(int id, string brand, string name, double caloriesPerGram)
    {
        var response = (await _httpClient.PostAsJsonAsync($"api/Food/Update/{id}",
                new UpdateFoodRequest(id, brand, name, caloriesPerGram)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FoodDto>() ??
               throw new JsonException("Failed to deserialize response");
    }

    public async Task DeleteFood(int id)
        => (await _httpClient.DeleteAsync($"api/Food/Delete/{id}"))
            .EnsureSuccessStatusCode();

    //
    // Feeding
    //

    public async Task<FeedingDto[]> GetAllFeedings()
        => await _httpClient.GetFromJsonAsync<FeedingDto[]>($"api/Feeding/GetAll") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<FeedingDto> GetOneFeeding(int id)
        => await _httpClient.GetFromJsonAsync<FeedingDto>($"api/Feeding/GetOne/{id}") ??
           throw new JsonException("Failed to deserialize response");

    public async Task<FeedingDto> CreateFeeding(int catId, int foodId, DateTimeOffset timestamp, double offered,
        double eaten)
    {
        var response = (await _httpClient.PutAsJsonAsync("api/Cat/Create",
                new CreateFeedingRequest(catId, foodId, timestamp, offered, eaten)))
            .EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FeedingDto>() ??
               throw new JsonException("Failed to deserialize response");
    }

    public async Task DeleteFeeding(int id)
        => (await _httpClient.DeleteAsync($"api/Feeding/Delete/{id}"))
            .EnsureSuccessStatusCode();

    //
    // Graphs
    //

    private string GetGraphUri(string path)
    {
        if (_environment.IsDevelopment())
        {
            var baseAddress = _httpClient.BaseAddress;
            if (baseAddress is null) throw new InvalidOperationException("API BaseAddress not set");

            var host = baseAddress.Host;
            var port = baseAddress.Port;
            var scheme = baseAddress.Scheme;

            // Development
            return $"{scheme}://{host}:{port}/{path}";
        }
        else
        {
            // Production / reverse-proxy
            return $"/{path}";
        }
    }

    private static string AddTimeFilterToPath(string path, DateTimeOffset? minTime, DateTimeOffset? maxTime)
    {
        const string format = "yyyy-MM-ddTHH:mm:ssZ";

        var pathBuilder = new StringBuilder(path);

        if (minTime != null)
            pathBuilder.Append($"&minTime={minTime.Value.ToString(format)}");

        if (maxTime != null)
            pathBuilder.Append($"&maxTime={maxTime.Value.ToString(format)}");

        return pathBuilder.ToString();
    }

    public string GetScaleEventGraphUri(int id)
    {
        return GetGraphUri($"api/Graph/GetScaleEvent?scaleEventId={id}");
    }

    public string GetCatGraphUri(int id, DateTimeOffset? minTime, DateTimeOffset? maxTime, bool includeTemperature)
    {
        var path = AddTimeFilterToPath($"api/Graph/GetCatMeasurements?catId={id}", minTime, maxTime);
        path += $"&includeTemperature={includeTemperature}";
        return GetGraphUri(path);
    }

    public string GetCombinedCatGraphUri(int id1, int id2, bool sameAxis, DateTimeOffset? minTime,
        DateTimeOffset? maxTime)
    {
        return GetGraphUri(AddTimeFilterToPath(
            $"api/Graph/GetCombinedCatMeasurements?catId1={id1}&catId2={id2}&sameAxis={sameAxis}", minTime, maxTime));
    }

    public string GetToiletGraphUri(int id, ToiletSensorValue sensorValue)
    {
        return GetGraphUri($"api/Graph/GetToiletData?toiletId={id}&sensorValue={sensorValue}");
    }

    public string GetCombinedToiletGraphUri(int id, ToiletSensorValue sensorValue1, ToiletSensorValue sensorValue2)
    {
        return GetGraphUri(
            $"api/Graph/GetCombinedToiletData?toiletId={id}&sensorValue1={sensorValue1}&sensorValue2={sensorValue2}");
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