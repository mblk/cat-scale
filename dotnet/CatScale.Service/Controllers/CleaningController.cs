using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Cleaning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class CleaningController : ControllerBase
{
    private readonly ILogger<CleaningController> _logger;
    private readonly CatScaleContext _dbContext;
    private readonly DataMapper _mapper = new();

    public CleaningController(ILogger<CleaningController> logger, CatScaleContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [Authorize(AuthenticationSchemes = "ApiKey")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewCleaning newCleaning)
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

        return CreatedAtAction(nameof(Create), new { Id = cleaning.Id }, cleaning);
    }
}
