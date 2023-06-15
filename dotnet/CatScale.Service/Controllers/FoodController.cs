using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Food;
using CatScale.Service.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class FoodController : ControllerBase
{
    private readonly ILogger<FoodController> _logger;
    private readonly IRepositoryWrapper _repositoryWrapper;

    public FoodController(ILogger<FoodController> logger, IRepositoryWrapper repositoryWrapper)
    {
        Console.WriteLine("FoodController ctor");
        _logger = logger;
        _repositoryWrapper = repositoryWrapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<FoodDto>> GetAll()
    {
        var foods = _repositoryWrapper.FoodRepository
            .Get()
            .AsEnumerable()
            .Select(DataMapper.MapFood)
            .ToArray();

        return Ok(foods);
    }

    [HttpGet]
    public ActionResult<FoodDto> GetOne(int id)
    {
        var food = _repositoryWrapper.FoodRepository
            .Get(x => x.Id == id)
            .SingleOrDefault();

        if (food is null)
            return NotFound();

        return Ok(DataMapper.MapFood(food));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPut]
    public async Task<ActionResult<FoodDto>> Create([FromBody] CreateFoodRequest request)
    {
        // Check if it already exists.
        if (_repositoryWrapper.FoodRepository
            .Get(f => f.Brand == request.Brand && f.Name == request.Name)
            .Any())
        {
            return BadRequest("Same food already exists");
        }
        
        _logger.LogInformation("New food: {food}", request);

        var newFood = new Food
        {
            Brand = request.Brand,
            Name = request.Name,
            CaloriesPerGram = request.CaloriesPerGram,
        };

        _repositoryWrapper.FoodRepository.Create(newFood);
        await _repositoryWrapper.Save();

        return CreatedAtAction(nameof(GetOne), 
            new { id = newFood.Id }, 
            DataMapper.MapFood(newFood));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<FoodDto>> Update([FromBody] UpdateFoodRequest request)
    {
        // TODO check if exists?
        
        _logger.LogInformation("Update food: {food}", request);

        var updatedFood = new Food()
        {
            Id = request.Id,
            Brand = request.Brand,
            Name = request.Name,
            CaloriesPerGram = request.CaloriesPerGram,
        };
        
        _repositoryWrapper.FoodRepository.Update(updatedFood);
        await _repositoryWrapper.Save();

        return Ok();
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Delete food: {id}", id);

        var food = _repositoryWrapper.FoodRepository
            .Get(x => x.Id == id)
            .SingleOrDefault();

        if (food is null)
            return NotFound();
        
        _repositoryWrapper.FoodRepository.Delete(food);
        await _repositoryWrapper.Save();

        return Ok();
    }
}