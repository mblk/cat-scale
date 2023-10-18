using System.Net;
using CatScale.Service.Model.Cat;

namespace CatScale.Service.Tests.Integration;

public class CatIntegrationTests : IntegrationTest
{
    public CatIntegrationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task GetAll_Should_ReturnEmptyList_When_NoCatsExist()
    {
        var cats = await GetAllCats();
        
        Assert.Empty(cats);
    }
    
    [Fact]
    public async Task GetAll_Should_ReturnCats_When_CatsExist()
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
    public async Task GetOne_Should_ReturnNotFound_When_CatDoesNotExist()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await GetCat(1));
        
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetOne_Should_ReturnCat_When_CatExists()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        await Logout();

        var returnedCat = await GetCat(createdCat.Id);
        
        Assert.Equal(createdCat.Id, returnedCat.Id);
        Assert.Equal("cat", returnedCat.Name);
        Assert.Equal(CatTypeDto.Active, returnedCat.Type);
        Assert.Equal(new DateOnly(1986, 5, 20), returnedCat.DateOfBirth);
    }
    
    [Fact]
    public async Task Create_Should_NotCreateCat_When_NotAuthorized()
    {
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await CreateCat(CatTypeDto.Active, "cat", 
                new DateOnly(1986, 5, 20)));
        
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Create_Should_CreateCat_When_Authorized()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", new DateOnly(1986, 5, 20));
        
        Assert.True(createdCat.Id > 0);
        Assert.Equal(CatTypeDto.Active, createdCat.Type);
        Assert.Equal("cat", createdCat.Name);
        Assert.Equal(new DateOnly(1986, 5, 20), createdCat.DateOfBirth);
    }

    [Fact]
    public async Task Update_Should_NotUpdateCat_When_NotAuthorized()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", 
            new DateOnly(1986, 5, 20));
        await Logout();
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await UpdateCat(createdCat.Id, CatTypeDto.Inactive, 
                "cat2", new DateOnly(2000, 1, 1)));
        
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }
    
    [Fact]
    public async Task Update_Should_UpdateCat_When_Authorized()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", 
            new DateOnly(1986, 5, 20));
        
        await UpdateCat(createdCat.Id, CatTypeDto.Inactive, "cat2", 
            new DateOnly(2000, 1, 1));
        
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
    public async Task Delete_Should_NotDeleteCat_When_NotAuthorized()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", 
            new DateOnly(1986, 5, 20));
        await Logout();
        
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            async () => await DeleteCat(createdCat.Id));
        
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }
    
    [Fact]
    public async Task Delete_Should_DeleteCat_When_Authorized()
    {
        await Login();
        var createdCat = await CreateCat(CatTypeDto.Active, "cat", 
            new DateOnly(1986, 5, 20));
        
        await DeleteCat(createdCat.Id);
        
        var cats = await GetAllCats();
        Assert.Empty(cats);
    }
}