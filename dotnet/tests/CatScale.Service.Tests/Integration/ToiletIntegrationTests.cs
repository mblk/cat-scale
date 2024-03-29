using System.Net;

namespace CatScale.Service.Tests.Integration;

public class ToiletIntegrationTests : IntegrationTest
{
    public ToiletIntegrationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task GetAll_Should_ReturnEmptyList_When_NoToiletsExist()
    {
        var toilets = await Toilet.GetAll();

        Assert.Empty(toilets);
    }

    [Fact]
    public async Task GetAll_Should_ReturnToilets_When_ToiletsExists()
    {
        await Login();
        await Toilet.Create("toilet1", "desc1");
        await Toilet.Create("toilet2", "desc2");
        await Logout();

        var toilets = await Toilet.GetAll();

        Assert.Collection(toilets, t =>
        {
            Assert.Equal("toilet1", t.Name);
            Assert.Equal("desc1", t.Description);
        }, t =>
        {
            Assert.Equal("toilet2", t.Name);
            Assert.Equal("desc2", t.Description);
        });
    }

    [Fact]
    public async Task GetOne_Should_ReturnNotFound_When_ToiletDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await Toilet.Get(1));

        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetOne_Should_ReturnToilet_When_ToiletExists()
    {
        await Login();
        var createdToilet = await Toilet.Create("toilet", "desc");
        await Logout();

        var returnedToilet = await Toilet.Get(createdToilet.Id);

        Assert.Equal(createdToilet.Id, returnedToilet.Id);
        Assert.Equal("toilet", returnedToilet.Name);
        Assert.Equal("desc", returnedToilet.Description);
    }

    [Fact]
    public async Task Create_Should_NotCreateToilet_When_NotAuthorized()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await Toilet.Create("toilet", "desc"));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Create_Should_CreateToilet_When_Authorized()
    {
        await Login();
        var createdToilet = await Toilet.Create("toilet", "desc");

        Assert.True(createdToilet.Id > 0);
        Assert.Equal("toilet", createdToilet.Name);
        Assert.Equal("desc", createdToilet.Description);
    }

    [Fact]
    public async Task Update_Should_NotUpdateToilet_When_NotAuthorized()
    {
        await Login();
        var createdToilet = await Toilet.Create("toilet", "desc");
        await Logout();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await Toilet.Update(createdToilet.Id, "toilet2", "desc2"));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Update_Should_UpdateToilet_When_Authorized()
    {
        await Login();
        var createdToilet = await Toilet.Create("toilet", "desc");

        await Toilet.Update(createdToilet.Id, "toilet2", "desc2");

        var toilets = await Toilet.GetAll();
        Assert.Collection(toilets, t =>
        {
            Assert.Equal(createdToilet.Id, t.Id);
            Assert.Equal("toilet2", t.Name);
            Assert.Equal("desc2", t.Description);
        });
    }

    [Fact]
    public async Task Delete_Should_NotDeleteToilet_When_NotAuthorized()
    {
        await Login();
        var createdToilet = await Toilet.Create("toilet", "desc");
        await Logout();

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await Toilet.Delete(createdToilet.Id));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Delete_Should_DeleteToilet_When_Authorized()
    {
        await Login();
        var createdToilet = await Toilet.Create("toilet", "desc");

        await Toilet.Delete(createdToilet.Id);

        var toilets = await Toilet.GetAll();
        Assert.Empty(toilets);
    }
}