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
public class CatController : ControllerBase
{
    private readonly ILogger<CatController> _logger;
    private readonly CatScaleContext _dbContext;

    public CatController(ILogger<CatController> logger, CatScaleContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet]
    public ActionResult<IEnumerable<CatDto>> GetAll()
    {
        _logger.LogInformation($"Getting all cats");
        
        var cats = _dbContext.Cats
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .AsEnumerable()
            .Select(DataMapper.MapCat);

        return Ok(cats);
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatDto>> GetOne(int id)
    {
        _logger.LogInformation($"Getting cat {id}");
        
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        Cat? cat = await _dbContext.Cats
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id);

        if (cat is null)
            return NotFound();
        
        return Ok(DataMapper.MapCat(cat));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPut]
    public async Task<ActionResult<CatDto>> Create([FromBody] CreateCatRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        string name = request.Name?.Trim() ?? String.Empty;
        if (String.IsNullOrWhiteSpace(name))
        {
            _logger.LogError($"Can't create cat because no name has been specified");
            return BadRequest("Invalid name");
        }

        if (await _dbContext.Cats.AnyAsync(c => c.Name == name))
        {
            _logger.LogError($"Can't create cat because the name '{name}' is already in use");
            return BadRequest("Name already in use");
        }
        
        _logger.LogInformation($"Creating new cat: {request}");
        
        var newCat = new Cat()
        {
            Name = name,
            DateOfBirth = request.DateOfBirth,
        };

        await _dbContext.Cats.AddAsync(newCat);
        await _dbContext.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetOne), new { id = newCat.Id }, DataMapper.MapCat(newCat));
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost("{id:int}")]
    public async Task<ActionResult<CatDto>> Update(int id, [FromBody] UpdateCatRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        Cat? existingCat = await _dbContext.Cats
            .SingleOrDefaultAsync(c => c.Id == id);

        if (existingCat is null)
        {
            _logger.LogError($"Can't update cat {id} because it does not exist");
            return NotFound();
        }

        string name = request.Name?.Trim() ?? String.Empty;
        if (String.IsNullOrWhiteSpace(name))
        {
            _logger.LogError($"Can't update cat because no name has been specified");
            return BadRequest("Invalid name");
        }

        if (await _dbContext.Cats.AnyAsync(c => c.Name == name && c.Id != existingCat.Id))
        {
            _logger.LogError($"Can't update cat because the name '{name}' is already in use");
            return BadRequest("Name already in use");
        }
        
        _logger.LogInformation($"Updating cat: {request}");
        
        existingCat.Name = name;
        existingCat.DateOfBirth = request.DateOfBirth;

        await _dbContext.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetOne), new { id = existingCat.Id }, DataMapper.MapCat(existingCat));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        Cat? existingCat = await _dbContext.Cats
            .SingleOrDefaultAsync(c => c.Id == id);

        if (existingCat is null)
        {
            _logger.LogError($"Can't delete cat {id} because it does not exist");
            return NotFound();
        }

        _logger.LogInformation($"Deleting cat {id}");
        
        _dbContext.Cats.Remove(existingCat);
        await _dbContext.SaveChangesAsync();
        
        return Ok();
    }
}