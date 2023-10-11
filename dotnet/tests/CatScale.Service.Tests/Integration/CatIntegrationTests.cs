using System.Net;
using CatScale.Service.Model.Cat;
using Xunit.Abstractions;

namespace CatScale.Service.Tests.Integration;

public class CatIntegrationTests : IntegrationTest
{
    public CatIntegrationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Should_ReturnEmptyList_When_NoCatsExist()
    {
        var cats = await GetAllCats();
        Assert.Empty(cats);
    }
    
    [Fact]
    public async Task Should_NotCreateCat_When_NotAuthorized()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await CreateCat(
            CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20)));
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Should_CreateCat_When_Authorized()
    {
        await Login();
        await CreateCat(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
    }
    
    [Fact]
    public async Task Should_ReturnCats_When_CatsExist()
    {
        await Login();
        await CreateCat(CatTypeDto.Active, "cat1", new DateOnly(1986, 5, 20));
        await CreateCat(CatTypeDto.Inactive, "cat2", new DateOnly(2022, 1, 5));

        var cats = await GetAllCats();
        
        Assert.Collection(cats, c =>
        {
            Assert.Equal(CatTypeDto.Active, c.Type);
            Assert.Equal("cat1", c.Name);
            Assert.Equal(new DateOnly(1986, 5, 20), c.DateOfBirth);
        }, c =>
        {
            Assert.Equal(CatTypeDto.Inactive, c.Type);
            Assert.Equal("cat2", c.Name);
            Assert.Equal(new DateOnly(2022, 1, 5), c.DateOfBirth);
        });
    }
    
    [Fact]
    public async Task Should_NotUpdateCat_When_NotAuthorized()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        await Logout();
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await UpdateCat(
            createdCat.Id, CatTypeDto.Inactive, "cat2", new DateOnly(2000, 1, 1)));
        
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }
    
    [Fact]
    public async Task Should_UpdateCat_When_Authorized()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        
        await UpdateCat(createdCat.Id, CatTypeDto.Inactive, "cat2", new DateOnly(2000, 1, 1));
        
        var cats = await GetAllCats();
        Assert.Collection(cats, c =>
        {
            Assert.Equal(createdCat.Id, c.Id);
            Assert.Equal(CatTypeDto.Inactive, c.Type);
            Assert.Equal("cat2", c.Name);
            Assert.Equal(new DateOnly(2000, 1, 1), c.DateOfBirth);
        });
    }
    
    [Fact]
    public async Task Should_NotDeleteCat_When_NotAuthorized()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        await Logout();
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await DeleteCat(createdCat.Id));
        
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }
    
    [Fact]
    public async Task Should_DeleteCat_When_Authorized()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        
        await DeleteCat(createdCat.Id);
        
        var cats = await GetAllCats();
        Assert.Empty(cats);
    }
}