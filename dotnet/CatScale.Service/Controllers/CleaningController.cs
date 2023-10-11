using CatScale.Service.DbModel;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class CleaningController : ControllerBase
{
    private readonly ILogger<CleaningController> _logger;
    private readonly CatScaleDbContext _dbContext;

    public CleaningController(ILogger<CleaningController> logger, CatScaleDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }
}
