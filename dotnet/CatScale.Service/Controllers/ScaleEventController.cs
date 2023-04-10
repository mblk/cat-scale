using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.ScaleEvent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ScaleEventController : ControllerBase
{
    private readonly ILogger<ScaleEventController> _logger;
    private readonly CatScaleContext _dbContext;

    public ScaleEventController(ILogger<ScaleEventController> logger, CatScaleContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }
    
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewScaleEvent newScaleEvent)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        _logger.LogInformation($"New cat scale event: {newScaleEvent}");
        
        var toilet = _dbContext.Toilets.SingleOrDefault(t => t.Id == newScaleEvent.ToiletId);
        if (toilet is null)
            return NotFound($"Toilet does not exist");

        //
        // TODO classify event as cleaning or measurement
        //
        
        var scaleEvent = new ScaleEvent()
        {
            Toilet = toilet,
            StartTime = newScaleEvent.StartTime.ToUniversalTime(),
            EndTime = newScaleEvent.EndTime.ToUniversalTime(),
            
            StablePhases = newScaleEvent.StablePhases
                .Select(x => new StablePhase()
                {
                    Timestamp = x.Timestamp.ToUniversalTime(),
                    Length = x.Length,
                    Value = x.Value
                }).ToList(),
        };

        await _dbContext.ScaleEvents.AddAsync(scaleEvent);
        await _dbContext.SaveChangesAsync();
        
        return CreatedAtAction(nameof(Create), new { Id = scaleEvent.Id }, scaleEvent);
    }

    [HttpGet]
    public ActionResult<IEnumerable<ScaleEventDto>> GetAll(int? toiletId)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = _dbContext.ScaleEvents
            .AsNoTracking()
            .Include(e => e.StablePhases)
            .Include(e => e.Measurement)
            .Include(e => e.Cleaning)
            .Select(DataMapper.MapScaleEvent)
            .ToArray();

        return Ok(result);
    }
}