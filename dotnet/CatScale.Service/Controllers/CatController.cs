using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Cat;
using CatScale.Service.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class CatController : ControllerBase
{
    private readonly ILogger<CatController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public CatController(ILogger<CatController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public ActionResult<IAsyncEnumerable<CatDto>> GetAll()
    {
        var cats = _unitOfWork.GetRepository<Cat>()
            .Query(order: x => x.OrderBy(c => c.Id))
            .Select(DataMapper.MapCat);

        return Ok(cats);
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatDto>> GetOne([FromRoute] int id)
    {
        var cat = await _unitOfWork
            .GetRepository<Cat>()
            .Query(c => c.Id == id)
            .SingleOrDefaultAsync();

        return cat switch
        {
            null => NotFound(),
            not null => Ok(DataMapper.MapCat(cat)),
        };
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPut]
    public async Task<ActionResult<CatDto>> Create([FromBody] CreateCatRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var repo = _unitOfWork.GetRepository<Cat>();
        
        var name = request.Name?.Trim() ?? String.Empty;
        if (String.IsNullOrWhiteSpace(name))
            return BadRequest("Invalid name");

        // TODO: Must use transaction?
        var nameAlreadyInUse = await repo
            .Query(c => c.Name == name)
            .AnyAsync();
        if (nameAlreadyInUse)
            return BadRequest("Name already in use");
       
        var newCat = new Cat
        {
            Type = DataMapper.MapCatType(request.Type),
            Name = name,
            DateOfBirth = request.DateOfBirth,
        };

        repo.Create(newCat);
        
        await _unitOfWork.SaveChangesAsync();
       
        return CreatedAtAction(nameof(GetOne), new { id = newCat.Id }, DataMapper.MapCat(newCat));
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost("{id:int}")]
    public async Task<ActionResult<CatDto>> Update([FromRoute] int id, [FromBody] UpdateCatRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var repo = _unitOfWork.GetRepository<Cat>();

        var existingCat = await repo
            .Query(c => c.Id == id)
            .SingleOrDefaultAsync();
        if (existingCat is null)
            return NotFound();

        var name = request.Name?.Trim() ?? String.Empty;
        if (String.IsNullOrWhiteSpace(name))
            return BadRequest("Invalid name");

        // TODO: Must use transaction?
        var newNameAlreadyInUse = await repo
            .Query(c => c.Name == name && c.Id != existingCat.Id)
            .AnyAsync();
        if (newNameAlreadyInUse)
            return BadRequest("Name already in use");
        
        existingCat.Type = DataMapper.MapCatType(request.Type);
        existingCat.Name = name;
        existingCat.DateOfBirth = request.DateOfBirth;

        repo.Update(existingCat);
        
        await _unitOfWork.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetOne), new { id = existingCat.Id }, DataMapper.MapCat(existingCat));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var repo = _unitOfWork.GetRepository<Cat>();

        var existingCat = await repo
            .Query(c => c.Id == id, includes: nameof(Cat.Measurements))
            .SingleOrDefaultAsync();
        
        if (existingCat is null)
            return NotFound();
        
        // Don't delete 'production' data by accident.
        var numMeasurements = existingCat.Measurements.Count;
        if (numMeasurements > 10)
        {
            _logger.LogError("Not deleting cat {CatId} because it has too many measurements {NumMeasurements}", id, numMeasurements);
            return BadRequest("Not deleting cat because it has too many measurements");
        }

        _logger.LogInformation("Deleting cat {CatId}", id);

        repo.Delete(existingCat);

        await _unitOfWork.SaveChangesAsync();
        
        return Ok();
    }
}