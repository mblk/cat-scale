using CatScale.Application.Repository;
using CatScale.Application.UseCases.CatWeights;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Cat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class CatWeightController : ControllerBase
{
    private readonly ILogger<CatWeightController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public CatWeightController(ILogger<CatWeightController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("{catId:int}")]
    public async Task<ActionResult<IAsyncEnumerable<CatWeightDto>>> GetAll([FromRoute] int catId)
    {
        var catWeights = (await new GetAllCatWeightsInteractor(_unitOfWork)
            .GetAllCatWeights(catId))
            .Select(DataMapper.MapCatWeight);

        return Ok(catWeights);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatWeightDto>> GetOne([FromRoute] int id)
    {
        var catWeight = await new GetCatWeightInteractor(_unitOfWork)
            .GetCatWeight(id);
        
        return Ok(DataMapper.MapCatWeight(catWeight));
    }

    [HttpPost]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public async Task<ActionResult<CatWeightDto>> Create([FromBody] CreateCatWeightRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var newCatWeight = await new CreateCatWeightInteractor(_unitOfWork)
            .CreateCatWeight(new CreateCatWeightInteractor.Request(
                request.CatId, request.Timestamp, request.Weight
            ));

        return CreatedAtAction(nameof(GetOne),
            new { id = newCatWeight.Id },
            DataMapper.MapCatWeight(newCatWeight));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = ApplicationRoles.Admin)]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        await new DeleteCatWeightInteractor(_unitOfWork)
            .DeleteCatWeight(id);

        return Ok();
    }
}