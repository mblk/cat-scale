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
    private readonly IRepositoryWrapper _repositoryWrapper;

    public FeedingController(ILogger<FeedingController> logger, IRepositoryWrapper repositoryWrapper)
    {
        _logger = logger;
        _repositoryWrapper = repositoryWrapper;
    }

    [HttpGet]
    public ActionResult<IEnumerable<FeedingDto>> GetAll()
    {
        var foods = _repositoryWrapper.FeedingRepository
            .Get()
            .AsEnumerable()
            .Select(DataMapper.MapFeeding)
            .ToArray();

        return Ok(foods);
    }

    [HttpGet]
    public ActionResult<FeedingDto> GetOne(int id)
    {
        var feeding = _repositoryWrapper.FeedingRepository
            .Get(x => x.Id == id)
            .SingleOrDefault();

        if (feeding is null)
            return NotFound();

        return Ok(DataMapper.MapFeeding(feeding));
    }

    [HttpPut]
    public async Task<ActionResult<FeedingDto>> Create([FromBody] CreateFeedingRequest request)
    {
        // Check cat/food/etc.
        
        // Check if it already exists.
        // if (_repositoryWrapper.FoodRepository
        //     .Get(f => f.Brand == request.Brand && f.Name == request.Name)
        //     .Any())
        // {
        //     return BadRequest("Same feeding already exists");
        // }
        
        _logger.LogInformation("New feeding: {food}", request);

        var newFeeding = new Feeding
        {
            CatId = request.CatId,
            FoodId = request.FoodId,
            Timestamp = request.Timestamp,
            Offered = request.Offered,
            Eaten = request.Eaten,
        };

        _repositoryWrapper.FeedingRepository.Create(newFeeding);
        await _repositoryWrapper.Save();

        return CreatedAtAction(nameof(GetOne), 
            new { id = newFeeding.Id }, 
            DataMapper.MapFeeding(newFeeding));
    }

    // [HttpPost]
    // public async Task<ActionResult<FeedingDto>> Update([FromBody] FeedingDto request)
    // {
    //     // TODO check if exists?
    //     
    //     _logger.LogInformation("Update feeding: {food}", request);
    //
    //     var updatedFeeding = new Feeding()
    //     {
    //         Id = request.Id,
    //         CatId = request.CatId,
    //         FoodId = request.FoodId,
    //         Timestamp = request.Timestamp,
    //         Offered = request.Offered,
    //         Eaten = request.Eaten,
    //     };
    //     
    //     _repositoryWrapper.FeedingRepository.Update(updatedFeeding);
    //     await _repositoryWrapper.Save();
    //
    //     return Ok();
    // }
    
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Delete feeding: {id}", id);
    
        var feeding = _repositoryWrapper.FeedingRepository
            .Get(x => x.Id == id)
            .SingleOrDefault();
    
        if (feeding is null)
            return NotFound();
        
        _repositoryWrapper.FeedingRepository.Delete(feeding);
        await _repositoryWrapper.Save();
    
        return Ok();
    }
}