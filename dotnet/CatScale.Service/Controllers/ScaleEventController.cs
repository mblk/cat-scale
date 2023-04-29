using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.ScaleEvent;
using CatScale.Service.Services;
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
    private readonly IClassificationService _classificationService;

    public ScaleEventController(ILogger<ScaleEventController> logger, CatScaleContext dbContext, IClassificationService classificationService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _classificationService = classificationService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScaleEventDto>>> GetAll(int? toiletId)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        _logger.LogInformation($"Getting all scale events");

        var scaleEvents = await _dbContext.ScaleEvents
            .AsNoTracking()
            .Include(e => e.StablePhases)
            .Include(e => e.Measurement)
            .ThenInclude(m => m!.Cat)
            .Include(e => e.Cleaning)
            .ToArrayAsync();
            
        var mappedScaleEvents = scaleEvents
            .Select(DataMapper.MapScaleEvent)
            .ToArray();

        return Ok(mappedScaleEvents);
    }
    
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewScaleEvent newScaleEvent)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        _logger.LogInformation($"New cat scale event: {newScaleEvent}");
        
        var toilet = await _dbContext.Toilets
            .SingleOrDefaultAsync(t => t.Id == newScaleEvent.ToiletId);
        
        if (toilet is null)
            return NotFound($"Toilet does not exist");
        
        var scaleEvent = new ScaleEvent()
        {
            ToiletId = toilet.Id,
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
        
        var cats = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Weights)
            .ToArrayAsync();
        
        _classificationService.ClassifyScaleEvent(cats, scaleEvent);

        await _dbContext.ScaleEvents.AddAsync(scaleEvent);
        await _dbContext.SaveChangesAsync();

        var scaleEventDto = DataMapper.MapScaleEvent(scaleEvent);
        
        return CreatedAtAction(nameof(Create), new { Id = scaleEventDto.Id }, scaleEventDto);
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        _logger.LogInformation($"Deleting scale event {id}");
        
        var scaleEvent = await _dbContext.ScaleEvents
            .SingleOrDefaultAsync(x => x.Id == id);
        
        if (scaleEvent is null)
            return NotFound();

        _dbContext.ScaleEvents.Remove(scaleEvent);
        await _dbContext.SaveChangesAsync();

        return Ok();
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost("{id:int}")]
    public async Task<IActionResult> Classify(int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        _logger.LogInformation($"Classifying scale event {id}");

        var cats = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Weights)
            .ToArrayAsync();

        var scaleEvent = await _dbContext.ScaleEvents
            .Include(x => x.StablePhases)
            .Include(x => x.Cleaning)
            .Include(x => x.Measurement)
            .SingleOrDefaultAsync(x => x.Id == id);
        
        if (scaleEvent is null)
            return NotFound();

        _classificationService.ClassifyScaleEvent(cats, scaleEvent);
        await _dbContext.SaveChangesAsync();
        
        return Ok();
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost]
    public async Task<IActionResult> ClassifyAllEvents()
    {
        _logger.LogInformation($"Classifying all scale events");

        await Task.Delay(TimeSpan.FromSeconds(5));
        
        var cats = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Weights)
            .ToArrayAsync();

        var scaleEvents = _dbContext.ScaleEvents
            .Include(x => x.StablePhases)
            .Include(x => x.Cleaning)
            .Include(x => x.Measurement);
        
        foreach (var scaleEvent in scaleEvents)
        {
            _classificationService.ClassifyScaleEvent(cats, scaleEvent);
        }

        await _dbContext.SaveChangesAsync();
        
        return Ok();
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost]
    public async Task<IActionResult> DeleteAllClassifications()
    {
        _logger.LogInformation($"Deleting all classifications");
        
        _dbContext.Cleanings.RemoveRange(_dbContext.Cleanings);
        _dbContext.Measurements.RemoveRange(_dbContext.Measurements);

        await _dbContext.SaveChangesAsync();
        
        return Ok();
    }
}