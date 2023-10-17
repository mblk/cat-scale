using System.Net;

namespace CatScale.Service.Tests.Integration;

public class CatWeightIntegrationTests : IntegrationTest
{
    public CatWeightIntegrationTests(ITestOutputHelper testOutputHelper)
        :base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CatDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await GetAllCatWeights(1));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CatWeightDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await GetCatWeight(1));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }
    
    
}