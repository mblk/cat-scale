using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Toilet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ToiletController : ControllerBase
{
    private readonly ILogger<ToiletController> _logger;
    private readonly CatScaleDbContext _dbContext;

    public ToiletController(ILogger<ToiletController> logger, CatScaleDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ToiletDto>> GetAll()
    {
        _logger.LogInformation($"Get all toilets");
        
        var toilets = _dbContext.Toilets
            .AsNoTracking()
            .AsEnumerable()
            .Select(DataMapper.MapToilet);

        return Ok(toilets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ToiletDto>> GetOne(int id)
    {
        _logger.LogInformation($"Get toilet {id}");

        Toilet? toilet = await _dbContext.Toilets
            .AsNoTracking()
            .Include(c => c.ScaleEvents).ThenInclude(e => e.StablePhases)
            .SingleOrDefaultAsync(c => c.Id == id);
        
        if (toilet is null)
            return NotFound();
        
        return Ok(DataMapper.MapToilet(toilet));
    }
}
