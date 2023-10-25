using System.Net;
using CatScale.Service.Model.Cat;

namespace CatScale.Service.Tests.Integration;

public class CatWeightIntegrationTests : IntegrationTest
{
    public CatWeightIntegrationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task GetAll_Should_ReturnNotFound_When_CatDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await CatWeight.GetAll(1));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetAll_Should_ReturnCatWeights_When_CatExists()
    {
        await Login();
        var createdCat = await Cat.Create(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        await CatWeight.Create(createdCat.Id, new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), 5000.0d);
        await CatWeight.Create(createdCat.Id, new DateTimeOffset(2020, 2, 1, 12, 0, 0, TimeSpan.FromHours(2)), 5100.0d);
        await Logout();

        var catWeights = await CatWeight.GetAll(createdCat.Id);

        Assert.Collection(catWeights, cw =>
        {
            Assert.Equal(5000.0d, cw.Weight, 0.001d);
            Assert.Equal(new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), cw.Timestamp);
        }, cw =>
        {
            Assert.Equal(5100.0d, cw.Weight, 0.001d);
            Assert.Equal(new DateTimeOffset(2020, 2, 1, 12, 0, 0, TimeSpan.FromHours(2)), cw.Timestamp);
        });
    }

    [Fact]
    public async Task GetOne_Should_ReturnNotFound_When_CatWeightDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await CatWeight.Get(1));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetOne_Should_ReturnCatWeight_When_CatWeightExists()
    {
        await Login();
        var createdCat = await Cat.Create(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        var createdCatWeight = await CatWeight.Create(createdCat.Id,
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), 5000.0d);
        await Logout();

        var returnedCatWeight = await CatWeight.Get(createdCatWeight.Id);

        Assert.Equal(5000.0d, returnedCatWeight.Weight, 0.001d);
        Assert.Equal(new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)),
            returnedCatWeight.Timestamp);
    }

    [Fact]
    public async Task Create_Should_NotCreateCatWeight_When_NotAuthorized()
    {
        await Login();
        var createdCat = await Cat.Create(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        await Logout();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await CatWeight.Create(createdCat.Id,
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), 5000.0d));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Create_Should_CreateCatWeight_When_Authorized()
    {
        await Login();
        var createdCat = await Cat.Create(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));

        var createdCatWeight = await CatWeight.Create(createdCat.Id,
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), 5000.0d);

        Assert.Equal(new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), createdCatWeight.Timestamp);
        Assert.Equal(5000.0d, createdCatWeight.Weight, 0.001d);
    }

    [Fact]
    public async Task Delete_Should_NotDeleteCatWeight_When_NotAuthorized()
    {
        await Login();
        var createdCat = await Cat.Create(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        var createdCatWeight = await CatWeight.Create(createdCat.Id,
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), 5000.0d);
        await Logout();

        var exception =
            await Assert.ThrowsAsync<HttpRequestException>(async () => await CatWeight.Delete(createdCatWeight.Id));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Delete_Should_NotDeleteCatWeight_When_NoMoreWeightsRemaining()
    {
        await Login();
        var createdCat = await Cat.Create(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        var createdCatWeight = await CatWeight.Create(createdCat.Id,
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), 5000.0d);

        var exception =
            await Assert.ThrowsAsync<HttpRequestException>(async () => await CatWeight.Delete(createdCatWeight.Id));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task Delete_Should_DeleteCatWeight_When_AuthorizedAndMoreWeightsRemaining()
    {
        await Login();
        var createdCat = await Cat.Create(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        await CatWeight.Create(createdCat.Id,
            new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), 5000.0d);
        var createdCatWeight = await CatWeight.Create(createdCat.Id,
                    new DateTimeOffset(2020, 2, 1, 12, 0, 0, TimeSpan.FromHours(2)), 5100.0d);

        await CatWeight.Delete(createdCatWeight.Id);

        var remainingCatWeights = await CatWeight.GetAll(createdCat.Id);
        Assert.Collection(remainingCatWeights, cw =>
        {
            Assert.Equal(new DateTimeOffset(2020, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), cw.Timestamp);
            Assert.Equal(5000.0d, cw.Weight, 0.001d);
        });
    }
}