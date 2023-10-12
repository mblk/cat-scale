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
    private readonly IUnitOfWork _unitOfWork;

    public FoodController(ILogger<FoodController> logger, IUnitOfWork unitOfWork)
    {
        Console.WriteLine("FoodController ctor");
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public ActionResult<IAsyncEnumerable<FoodDto>> GetAll()
    {
        var foods = _unitOfWork.GetRepository<Food>()
            .Query()
            .Select(DataMapper.MapFood);

        return Ok(foods);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FoodDto>> GetOne([FromRoute] int id)
    {
        var food = await _unitOfWork.GetRepository<Food>()
            .Query(x => x.Id == id)
            .SingleOrDefaultAsync();

        return food switch
        {
            null => NotFound(),
            not null => DataMapper.MapFood(food),
        };
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPut]
    public async Task<ActionResult<FoodDto>> Create([FromBody] CreateFoodRequest request)
    {
        var repo = _unitOfWork.GetRepository<Food>();

        // TODO trim
        var foodAlreadyExists = await repo
            .Query(f => f.Brand == request.Brand && f.Name == request.Name)
            .AnyAsync();
        
        if (foodAlreadyExists)
            return BadRequest("Same food already exists");
        
        _logger.LogInformation("New food: {Food}", request);

        var newFood = new Food
        {
            Brand = request.Brand,
            Name = request.Name,
            CaloriesPerGram = request.CaloriesPerGram,
        };

        repo.Create(newFood);
        
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), 
            new { id = newFood.Id }, 
            DataMapper.MapFood(newFood));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<FoodDto>> Update([FromBody] UpdateFoodRequest request)
    {
        // TODO id from route?
        // TODO check if exists?
        
        var repo = _unitOfWork.GetRepository<Food>();
        
        _logger.LogInformation("Update food: {Food}", request);

        var updatedFood = new Food()
        {
            Id = request.Id,
            Brand = request.Brand,
            Name = request.Name,
            CaloriesPerGram = request.CaloriesPerGram,
        };
        
        repo.Update(updatedFood);
        
        await _unitOfWork.SaveChangesAsync();

        return Ok();
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        _logger.LogInformation("Delete food: {Id}", id);

        var repo = _unitOfWork.GetRepository<Food>();
        
        var food = await repo
            .Query(x => x.Id == id)
            .SingleOrDefaultAsync();

        if (food is null)
            return NotFound();
        
        repo.Delete(food);
        
        await _unitOfWork.SaveChangesAsync();

        return Ok();
    }
}