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
    public IActionResult Get()
    {
        var cats = _dbContext.Cats
            .Include(c => c.Weights)
            .Include(c => c.Measurements);

        return Ok(cats);
    }
}