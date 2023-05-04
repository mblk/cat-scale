using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Cat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class CatWeightController : ControllerBase
{
    private readonly ILogger<CatWeightController> _logger;
    private readonly CatScaleContext _dbContext;

    public CatWeightController(ILogger<CatWeightController> logger, CatScaleContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet("{catId:int}")]
    public async Task<ActionResult<IEnumerable<CatWeightDto>>> GetAll(int catId)
    {
        var cat = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Weights)
            .SingleOrDefaultAsync(c => c.Id == catId);

        if (cat is null)
            return NotFound();

        var mappedWeights = cat.Weights
            .Select(DataMapper.MapCatWeight);

        return Ok(mappedWeights);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatWeightDto>> GetOne(int id)
    {
        var catWeight = await _dbContext.CatWeights
            .SingleOrDefaultAsync(cw => cw.Id == id);

        if (catWeight is null)
            return NotFound();

        return Ok(DataMapper.MapCatWeight(catWeight));
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public async Task<ActionResult<CatWeightDto>> Create([FromBody] CreateCatWeightRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        _logger.LogInformation("Creating new cat weight {CatWeight}", request);
        
        var cat = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Weights)
            .SingleOrDefaultAsync(c => c.Id == request.CatId);

        if (cat is null)
            return BadRequest("Cat does not exist");

        var newCatWeight = new CatWeight()
        {
            CatId = cat.Id,
            Timestamp = request.Timestamp.ToUniversalTime(),
            Weight = request.Weight,
        };

        await _dbContext.CatWeights.AddAsync(newCatWeight);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), new { Id = newCatWeight.Id }, DataMapper.MapCatWeight(newCatWeight));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Deleting cat weight {id}", id);

        var catWeight = await _dbContext.CatWeights
            .SingleOrDefaultAsync(cw => cw.Id == id);

        if (catWeight is null)
            return NotFound("Cat weight does not exist");

        var cat = await _dbContext.Cats
            .Include(c => c.Weights)
            .SingleOrDefaultAsync(c => c.Id == catWeight.CatId);

        if (cat is null)
            return NotFound("Cat does not exist");

        if (cat.Weights.Count <= 1)
            return BadRequest("Can't delete last remaining cat weight");

        _dbContext.CatWeights.Remove(catWeight);
        await _dbContext.SaveChangesAsync();
        
        return Ok();
    }
    
    // get active
    // update

}