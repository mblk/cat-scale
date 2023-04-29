using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Cat;
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
        Cat? cat = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Weights)
            .SingleOrDefaultAsync(c => c.Id == catId);

        if (cat is null)
            return NotFound();

        var mappedWeights = cat.Weights
            .Select(DataMapper.MapCatWeight);

        return Ok(mappedWeights);
    }
    
    // getactive
    
    // create
    // delete
    // update
    

}