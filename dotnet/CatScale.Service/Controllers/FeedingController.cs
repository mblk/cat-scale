using CatScale.Domain.Model;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Food;
using CatScale.Service.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class FeedingController : ControllerBase
{
    private readonly ILogger<FeedingController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public FeedingController(ILogger<FeedingController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public ActionResult<IEnumerable<FeedingDto>> GetAll()
    {
        var feedings = _unitOfWork.FeedingRepository
            .Get()
            .AsEnumerable()
            .Select(DataMapper.MapFeeding)
            .ToArray();

        return Ok(feedings);
    }

    [HttpGet]
    public ActionResult<FeedingDto> GetOne(int id)
    {
        var feeding = _unitOfWork.FeedingRepository
            .Get(x => x.Id == id)
            .SingleOrDefault();

        if (feeding is null)
            return NotFound();

        return Ok(DataMapper.MapFeeding(feeding));
    }

    [HttpPut]
    public async Task<ActionResult<FeedingDto>> Create([FromBody] CreateFeedingRequest request)
    {
        // TODO Check cat/food/etc?
        
        _logger.LogInformation("New feeding: {Feeding}", request);

        var newFeeding = new Feeding
        {
            CatId = request.CatId,
            FoodId = request.FoodId,
            Timestamp = request.Timestamp,
            Offered = request.Offered,
            Eaten = request.Eaten,
        };

        _unitOfWork.FeedingRepository.Create(newFeeding);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), 
            new { id = newFeeding.Id }, 
            DataMapper.MapFeeding(newFeeding));
    }
    
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Delete feeding: {Id}", id);
    
        var feeding = _unitOfWork.FeedingRepository
            .Get(x => x.Id == id)
            .SingleOrDefault();
    
        if (feeding is null)
            return NotFound();
        
        _unitOfWork.FeedingRepository.Delete(feeding);
        await _unitOfWork.SaveChangesAsync();
    
        return Ok();
    }
}