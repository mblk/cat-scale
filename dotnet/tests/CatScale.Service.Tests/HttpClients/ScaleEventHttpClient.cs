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

    public async Task<ScaleEventDto[]> GetAll()
        => await _client.GetFromJsonAsync<ScaleEventDto[]>($"api/ScaleEvent/GetAll") ??
           throw new Exception("Failed to deserialize response");

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

// public async Task DeleteScaleEvent(int id)
//     => (await _client.DeleteAsync($"api/ScaleEvent/Delete/{id}"))
//         .EnsureSuccessStatusCode();
//
// public async Task ClassifyScaleEvent(int id)
//     => (await _client.PostAsync($"api/ScaleEvent/Classify/{id}", null))
//         .EnsureSuccessStatusCode();
//
// public async Task ClassifyAllScaleEvents()
//     => (await _client.PostAsync($"api/ScaleEvent/ClassifyAllEvents", null))
//         .EnsureSuccessStatusCode();
//
// public async Task DeleteAllScaleEventClassifications()
//     => (await _client.PostAsync($"api/ScaleEvent/DeleteAllClassifications", null))
//         .EnsureSuccessStatusCode();
//
// public async Task<ScaleEventStats> GetScaleEventStats()
//     => await _client.GetFromJsonAsync<ScaleEventStats>("api/ScaleEvent/GetStats") ??
//        throw new Exception("Failed to deserialize response");
//
// public async Task<PooCount[]> GetPooCounts()
//     => await _client.GetFromJsonAsync<PooCount[]>("api/ScaleEvent/GetPooCounts") ??
//        throw new Exception("Failed to deserialize response");}
}