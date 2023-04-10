using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Toilet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ToiletController : ControllerBase
{
    private readonly ILogger<ToiletController> _logger;
    private readonly CatScaleContext _dbContext;

    public ToiletController(ILogger<ToiletController> logger, CatScaleContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ToiletDto>> GetAll()
    {
        var toilets = _dbContext.Toilets
            .AsNoTracking()
            // .Include(c => c.Cleanings)
            // .Include(c => c.Measurements)
            .AsEnumerable()
            .Select(DataMapper.MapToilet);

        return Ok(toilets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ToiletDto>> GetOne(int id, ToiletDetails? details)
    {
        _logger.LogInformation($"Get toilet {id} details {details}");

        Toilet? toilet;
        
        switch (details)
        {
            case null:
            case ToiletDetails.None:
            default:
            {
                toilet = await _dbContext.Toilets
                    .AsNoTracking()
                    .SingleOrDefaultAsync(c => c.Id == id);
                break;
            }

            //case ToiletDetails.All:
            //{
                // toilet = await _dbContext.Toilets
                //     .AsNoTracking()
                //     .Include(c => c.Cleanings)
                //     .Include(c => c.Measurements)
                //     .SingleOrDefaultAsync(c => c.Id == id);
                //break;
            //}

            //case ToiletDetails.SinceLastCleaning:
            //{
                // toilet = await _dbContext.Toilets
                //     .AsNoTracking()
                //     .Include(c => c.Cleanings)
                //     .Include(c => c.Measurements)
                //     .SingleOrDefaultAsync(c => c.Id == id);
                //
                // if (toilet != null)
                // {
                //     var lastCleaning = toilet.Cleanings.MaxBy(c => c.Timestamp);
                //     var timeOfLastCleaning = lastCleaning?.Timestamp ?? DateTimeOffset.MinValue;
                //     var measurementsSinceLastCleaning =
                //         toilet.Measurements.Where(c => c.Timestamp > timeOfLastCleaning).ToList();
                //     
                //     toilet = new Toilet()
                //     {
                //         Id = toilet.Id,
                //         Name = toilet.Name,
                //         Description = toilet.Description,
                //         Cleanings = new List<Cleaning>(),
                //         Measurements = measurementsSinceLastCleaning,
                //     };
                //     
                //     if (lastCleaning != null) toilet.Cleanings.Add(lastCleaning);
                // }
                
                //break;
            //}
        }
        
        if (toilet is null)
            return NotFound();
        return Ok(DataMapper.MapToilet(toilet));
    }
}
