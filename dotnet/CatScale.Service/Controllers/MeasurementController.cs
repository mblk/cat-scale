using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Model.Measurement;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize(AuthenticationSchemes = "ApiKey")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] NewMeasurement newMeasurement)
    {
        _logger.LogInformation("New measurement {measurement}", newMeasurement);

        var catsWithCurrentWeight = _dbContext.Cats
            .Include(c => c.Weights)
            .Where(c => c.Weights.Any())
            .Select(c => new
            {
                Cat = c,
                CurrentWeight = c.Weights.OrderByDescending(w => w.Timestamp).First()
            });

        var catsWithWeightDiff = catsWithCurrentWeight.Select(c => new
        {
            Cat = c.Cat,
            WeightDiff = Math.Abs(newMeasurement.CatWeight - c.CurrentWeight.Weight)
        });

        var classifiedCat = catsWithWeightDiff.OrderBy(c => c.WeightDiff).First().Cat;

        _logger.LogInformation($"Classified cat '{classifiedCat.Name}' from weight {newMeasurement.CatWeight}");
        
        var toilet = _dbContext.Toilets.SingleOrDefault(t => t.Id == newMeasurement.ToiletId);
        if (toilet is null)
            return NotFound($"Toilet does not exist");

        var measurement = new Measurement()
        {
            Cat = classifiedCat,
            Timestamp = newMeasurement.Timestamp.ToUniversalTime(),
            Toilet = toilet,
            SetupTime = newMeasurement.SetupTime,
            PooTime = newMeasurement.PooTime,
            CleanupTime = newMeasurement.CleanupTime,
            CatWeight = newMeasurement.CatWeight,
            PooWeight = newMeasurement.PooWeight
        };

        await _dbContext.Measurements.AddAsync(measurement);
        await _dbContext.SaveChangesAsync();
        
        return CreatedAtAction(nameof(Create), new { Id = measurement.Id }, measurement);
    }
}