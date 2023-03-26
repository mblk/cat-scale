using CatScale.Domain.Enums;
using CatScale.Domain.Model;

namespace CatScale.UI.BlazorServer.Data;

public interface ICatScaleService
{
    Task<Toilet[]> GetToiletsList();
    Task<Toilet> GetToiletDetails(int id, ToiletDetails details);

    Task<Cat[]> GetCatsList();
    Task<Cat> GetCatDetails(int id, CatDetails details);
}

public class CatScaleService : ICatScaleService
{
    private readonly HttpClient _httpClient;

    public CatScaleService(IConfiguration configuration)
    {
        var serviceAddr = configuration.GetValue<string>("CatScaleServiceAddr");
        if (String.IsNullOrWhiteSpace(serviceAddr)) throw new ArgumentException("invalid configuration");

        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(serviceAddr),
        };
    }

    public async Task<Toilet[]> GetToiletsList()
    {
        //var response = await _httpClient.GetAsync("http://localhost:5155/Toilet");
        var response = await _httpClient.GetAsync("Toilet");
        response.EnsureSuccessStatusCode();

        var toilets = await response.Content.ReadFromJsonAsync<Toilet[]>();
        if (toilets is null) throw new Exception("kaputt");

        return toilets;
    }

    public async Task<Toilet> GetToiletDetails(int id, ToiletDetails details)
    {
        //var response = await _httpClient.GetAsync($"http://localhost:5155/Toilet/{id}?details={details}");
        var response = await _httpClient.GetAsync($"Toilet/{id}?details={details}");
        response.EnsureSuccessStatusCode();
        
        var toilet = await response.Content.ReadFromJsonAsync<Toilet>();
        if (toilet is null) throw new Exception("kaputt");

        return toilet;
    }

    public async Task<Cat[]> GetCatsList()
    {
        //var response = await _httpClient.GetAsync("http://localhost:5155/Cat");
        var response = await _httpClient.GetAsync("Cat");
        response.EnsureSuccessStatusCode();

        var cats = await response.Content.ReadFromJsonAsync<Cat[]>();
        if (cats is null) throw new Exception("kaputt");

        return cats;
    }

    public async Task<Cat> GetCatDetails(int id, CatDetails details)
    {
        //var response = await _httpClient.GetAsync($"http://localhost:5155/Cat/{id}?details={details}");
        var response = await _httpClient.GetAsync($"Cat/{id}?details={details}");
        response.EnsureSuccessStatusCode();
        
        var cat = await response.Content.ReadFromJsonAsync<Cat>();
        if (cat is null) throw new Exception("kaputt");

        return cat;
    }
}