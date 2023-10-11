using CatScale.Service.Model.Authentication;
using CatScale.Service.Model.Cat;
using CatScale.Service.Tests.Utils;
using Xunit.Abstractions;

namespace CatScale.Service.Tests.Integration;

public abstract class IntegrationTest
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    
    protected ITestOutputHelper Output { get; }
    
    protected HttpClient Client { get; }

    protected IntegrationTest(ITestOutputHelper testOutputHelper)
    {
        Output = testOutputHelper;
        Console.SetOut(new ConsoleOutputRedirector(testOutputHelper));
        _factory = new CustomWebApplicationFactory<Program>();
        Client = _factory.CreateClient();
    }

    protected async Task Login()
    {
        var request = new LoginRequest()
        {
            UserName = "Admin",
            Password = "password",
        };

        (await Client.PostAsJsonAsync("api/Authentication/Login", request))
            .EnsureSuccessStatusCode();
    }

    protected async Task Logout()
    {
        (await Client.PostAsync("api/Authentication/Logout", null))
            .EnsureSuccessStatusCode();
    }
    
    protected async Task<CatDto[]> GetAllCats()
        => await Client.GetFromJsonAsync<CatDto[]>("api/Cat/GetAll") 
           ?? throw new Exception("Failed to deserialize response");
    
    protected async Task<CatDto> CreateCat(CatTypeDto type, string name, DateOnly dateOfBirth)
    {
        var request = new CreateCatRequest(type, name, dateOfBirth);
        var response = (await Client.PutAsJsonAsync("api/Cat/Create", request))
            .EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<CatDto>() ??
               throw new Exception("Failed to deserialize response");
    }

    protected async Task<CatDto> UpdateCat(int id, CatTypeDto type, string name, DateOnly dateOfBirth)
    {
        var response = (await Client.PostAsJsonAsync($"api/Cat/Update/{id}",
                new UpdateCatRequest(type, name, dateOfBirth)))
            .EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<CatDto>() ??
               throw new Exception("Failed to deserialize response");
    }
    
    protected async Task DeleteCat(int id)
        => (await Client.DeleteAsync($"api/Cat/Delete/{id}"))
            .EnsureSuccessStatusCode();
}