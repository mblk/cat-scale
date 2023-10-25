using CatScale.Service.Model.Authentication;
using CatScale.Service.Tests.HttpClients;
using CatScale.Service.Tests.Utils;

namespace CatScale.Service.Tests.Integration;

public abstract class IntegrationTest : IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ConsoleOutputRedirector _outputRedirector;

    protected ITestOutputHelper Output { get; }

    protected UserHttpClient User { get; }
    protected ToiletHttpClient Toilet { get; }
    protected CatHttpClient Cat { get; }
    protected CatWeightHttpClient CatWeight { get; }
    protected ScaleEventHttpClient ScaleEvent { get; }

    protected IntegrationTest(ITestOutputHelper testOutputHelper)
    {
        _factory = new CustomWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        
        _outputRedirector = new ConsoleOutputRedirector(testOutputHelper);
        Output = testOutputHelper;
        Console.SetOut(_outputRedirector);

        User = new UserHttpClient(_client);
        Toilet = new ToiletHttpClient(_client);
        Cat = new CatHttpClient(_client);
        CatWeight = new CatWeightHttpClient(_client);
        ScaleEvent = new ScaleEventHttpClient(_client, Output);
    }

    public void Dispose()
    {
        _outputRedirector.Dispose();
        _client.Dispose();
        _factory.Dispose();
    }

    protected async Task Login()
    {
        var request = new LoginRequest()
        {
            UserName = "Admin",
            Password = "password",
        };

        (await _client.PostAsJsonAsync("api/Authentication/Login", request))
            .EnsureSuccessStatusCode();
    }

    protected async Task Logout()
    {
        (await _client.PostAsync("api/Authentication/Logout", null))
            .EnsureSuccessStatusCode();
    }
}