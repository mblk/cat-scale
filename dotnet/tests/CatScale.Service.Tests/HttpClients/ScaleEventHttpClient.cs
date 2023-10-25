using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CatScale.Service.Model.ScaleEvent;

namespace CatScale.Service.Tests.HttpClients;

public class ScaleEventHttpClient
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public ScaleEventHttpClient(HttpClient client, ITestOutputHelper output)
    {
        _client = client;
        _output = output;
    }

    public async Task<ScaleEventDto[]> GetAll(int? toiletId = null, int? skip = null, int? take = null)
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

        return await _client.GetFromJsonAsync<ScaleEventDto[]>(uri) ??
               throw new Exception("Failed to deserialize response");
    }

    public async Task<ScaleEventDto> Get(int id)
        => await _client.GetFromJsonAsync<ScaleEventDto>($"api/ScaleEvent/GetOne/{id}")
           ?? throw new Exception("Failed to deserialize response");

    public async Task<ScaleEventDto> Create(NewScaleEvent scaleEvent)
    {
        var response = await _client.PostAsJsonAsync("api/ScaleEvent/Create", scaleEvent);
        _output.WriteLine($"Http status: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Http response: {content}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ScaleEventDto>() ??
               throw new Exception("Failed to deserialize response");
    }

    public async Task<ScaleEventDto> CreateSimpleMeasurement(int toiletId, DateTimeOffset startTime)
    {
        var tStart = startTime;
        var tStable = startTime.AddSeconds(30); // 20-30
        var tEnd = startTime.AddSeconds(60);

        return await Create(new NewScaleEvent(toiletId, tStart, tEnd,
            new NewStablePhase[]
            {
                new(tStable, 10.0d, 5000.0d)
            },
            22.0d, 50.0d, 100000.0d));
    }

    public async Task<ScaleEventDto> CreateSimpleCleaning(int toiletId, DateTimeOffset startTime)
    {
        var tStart = startTime;
        var tStable1 = startTime.AddSeconds(30); // 20-30
        var tStable2 = startTime.AddSeconds(50); // 40-50
        var tEnd = startTime.AddSeconds(60);

        return await Create(new NewScaleEvent(toiletId, tStart, tEnd,
            new NewStablePhase[]
            {
                new(tStable1, 10.0d, -1000.0d),
                new(tStable2, 10.0d, -100.0d)
            },
            22.0d, 50.0d, 100000.0d));
    }

    public async Task<ScaleEventDto> CreateWithApiKey(NewScaleEvent scaleEvent, string apiKey)
    {
        var httpRequestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("api/ScaleEvent/Create", UriKind.Relative),
            Headers =
            {
                { HttpRequestHeader.Authorization.ToString(), "ApiKey" },
                { "ApiKey", apiKey },
                { HttpRequestHeader.Accept.ToString(), "application/json" },
                { HttpRequestHeader.ContentType.ToString(), "application/json" },
            },
            Content = new StringContent(
                JsonSerializer.Serialize(scaleEvent),
                new MediaTypeHeaderValue("application/json")),
        };

        var response = await _client.SendAsync(httpRequestMessage);
        _output.WriteLine($"Http status: {response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Http response: {content}");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ScaleEventDto>() ??
               throw new Exception("Failed to deserialize response");
    }

    public async Task Delete(int id)
        => (await _client.DeleteAsync($"api/ScaleEvent/Delete/{id}"))
            .EnsureSuccessStatusCode();

    public async Task Classify(int id)
        => (await _client.PostAsync($"api/ScaleEvent/Classify/{id}", null))
            .EnsureSuccessStatusCode();

    public async Task<ScaleEventStats> GetStats()
        => await _client.GetFromJsonAsync<ScaleEventStats>("api/ScaleEvent/GetStats") ??
           throw new Exception("Failed to deserialize response");

    public async Task<PooCount[]> GetPooCounts()
        => await _client.GetFromJsonAsync<PooCount[]>("api/ScaleEvent/GetPooCounts") ??
           throw new Exception("Failed to deserialize response");
}