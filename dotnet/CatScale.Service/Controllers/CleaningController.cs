using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.RestModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("[controller]")]
public class CleaningController : ControllerBase
{
    private readonly ILogger<CleaningController> _logger;
    private readonly CatScaleContext _dbContext;

    public CleaningController(ILogger<CleaningController> logger, CatScaleContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Cleaning>> Get()
    {
        var cleanings = _dbContext.Cleanings;

        return Ok(cleanings);
    }
    
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] NewCleaning newCleaning)
    {
        _logger.LogInformation("New cleaning {cleaning}", newCleaning);

        var toilet = _dbContext.Toilets.SingleOrDefault(t => t.Id == newCleaning.ToiletId);
        if (toilet is null)
            return NotFound($"Toilet does not exist");
        
        var cleaning = new Cleaning()
        {
            Timestamp = newCleaning.Timestamp.ToUniversalTime(),
            Time = newCleaning.CleaningTime,
            Weight = newCleaning.CleaningWeight,
            Toilet = toilet,
        };
        
        await _dbContext.Cleanings.AddAsync(cleaning);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(Post), new { Id = cleaning.Id }, cleaning);
    }
}
