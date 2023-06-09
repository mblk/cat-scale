using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Measurement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class MeasurementController : ControllerBase
{
    private readonly ILogger<MeasurementController> _logger;
    private readonly CatScaleContext _dbContext;

    public MeasurementController(ILogger<MeasurementController> logger, CatScaleContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }
    
    [HttpGet("{catId:int}")]
    public async Task<ActionResult<IEnumerable<MeasurementDto>>> GetAll(int catId)
    {
        Cat? cat = await _dbContext.Cats
            .AsNoTracking()
            .Include(c => c.Measurements)
            .SingleOrDefaultAsync(c => c.Id == catId);

        if (cat is null)
            return NotFound();

        var mappedMeasurements = cat.Measurements
            .Select(DataMapper.MapMeasurement);

        return Ok(mappedMeasurements);
    }
}