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
    public ActionResult<IAsyncEnumerable<FeedingDto>> GetAll()
    {
        var feedings = _unitOfWork
            .GetRepository<Feeding>()
            .Query()
            .Select(DataMapper.MapFeeding);

        return Ok(feedings);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FeedingDto>> GetOne([FromRoute] int id)
    {
        var feeding = await _unitOfWork
            .GetRepository<Feeding>()
            .Query(x => x.Id == id)
            .SingleOrDefaultAsync();

        return feeding switch
        {
            null => NotFound(),
            not null => Ok(DataMapper.MapFeeding(feeding)),
        };
    }

    // TODO check
    [Authorize(AuthenticationSchemes = "ApiKey,Cookies", Roles = ApplicationRoles.Admin)]
    [HttpPut]
    public async Task<ActionResult<FeedingDto>> Create([FromBody] CreateFeedingRequest request)
    {
        // TODO Check cat/food/etc?
        
        var repo = _unitOfWork.GetRepository<Feeding>();

        var newFeeding = new Feeding
        {
            CatId = request.CatId,
            FoodId = request.FoodId,
            Timestamp = request.Timestamp,
            Offered = request.Offered,
            Eaten = request.Eaten,
        };

        repo.Create(newFeeding);
        
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), 
            new { id = newFeeding.Id }, 
            DataMapper.MapFeeding(newFeeding));
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        _logger.LogInformation("Delete feeding: {Id}", id);
    
        var repo = _unitOfWork.GetRepository<Feeding>();
        
        var feeding = await repo
            .Query(x => x.Id == id)
            .SingleOrDefaultAsync();
    
        if (feeding is null)
            return NotFound();
        
        repo.Delete(feeding);
        
        await _unitOfWork.SaveChangesAsync();
    
        return Ok();
    }
}