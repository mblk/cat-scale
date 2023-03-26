using CatScale.Domain.Enums;
using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("[controller]")]
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
    public IActionResult GetAll()
    {
        var cats = _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Weights)
            .Include(c => c.Measurements);

        return Ok(cats);
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cat>> GetOne(int id, CatDetails? details = null)
    {
        Cat? cat = null;
        
        switch (details)
        {
            case null:
            case CatDetails.None:
            default:
                cat = await _dbContext.Cats
                    .AsNoTracking()
                    .SingleOrDefaultAsync(c => c.Id == id);
                break;
            
            case CatDetails.All:
                cat = await _dbContext.Cats
                    .AsNoTracking()
                    .Include(c => c.Weights)
                    .Include(c => c.Measurements)
                    .SingleOrDefaultAsync(c => c.Id == id);
                break;
        }

        return cat is null ? NotFound() : Ok(cat);
    }
}