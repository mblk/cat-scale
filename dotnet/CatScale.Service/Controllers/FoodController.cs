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
    public ActionResult<IEnumerable<FoodDto>> GetAll()
    {
        var foods = _unitOfWork.FoodRepository
            .Get()
            .AsEnumerable()
            .Select(DataMapper.MapFood)
            .ToArray();

        return Ok(foods);
    }

    [HttpGet]
    public ActionResult<FoodDto> GetOne(int id)
    {
        var food = _unitOfWork.FoodRepository
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
        if (_unitOfWork.FoodRepository
            .Get(f => f.Brand == request.Brand && f.Name == request.Name)
            .Any())
        {
            return BadRequest("Same food already exists");
        }
        
        _logger.LogInformation("New food: {Food}", request);

        var newFood = new Food
        {
            Brand = request.Brand,
            Name = request.Name,
            CaloriesPerGram = request.CaloriesPerGram,
        };

        _unitOfWork.FoodRepository.Create(newFood);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), 
            new { id = newFood.Id }, 
            DataMapper.MapFood(newFood));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<FoodDto>> Update([FromBody] UpdateFoodRequest request)
    {
        // TODO check if exists?
        
        _logger.LogInformation("Update food: {Food}", request);

        var updatedFood = new Food()
        {
            Id = request.Id,
            Brand = request.Brand,
            Name = request.Name,
            CaloriesPerGram = request.CaloriesPerGram,
        };
        
        _unitOfWork.FoodRepository.Update(updatedFood);
        await _unitOfWork.SaveChangesAsync();

        return Ok();
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Delete food: {Id}", id);

        var food = _unitOfWork.FoodRepository
            .Get(x => x.Id == id)
            .SingleOrDefault();

        if (food is null)
            return NotFound();
        
        _unitOfWork.FoodRepository.Delete(food);
        await _unitOfWork.SaveChangesAsync();

        return Ok();
    }
}