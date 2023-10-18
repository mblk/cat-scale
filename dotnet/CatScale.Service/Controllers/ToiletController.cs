using CatScale.Application.UseCases.Toilets;
using CatScale.Domain.Model;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Toilet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ToiletController : ControllerBase
{
    private readonly ILogger<ToiletController> _logger;

    public ToiletController(ILogger<ToiletController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IAsyncEnumerable<ToiletDto>>> GetAll(
        [FromServices] IGetAllToiletsInteractor interactor)
    {
        var response = await interactor
            .GetAllToilets(new IGetAllToiletsInteractor.Request());

        var toilets = response.Toilets
            .Select(DataMapper.MapToilet);

        return Ok(toilets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ToiletDto>> GetOne(
        [FromServices] IGetOneToiletInteractor interactor,
        [FromRoute] int id)
    {
        var response = await interactor
            .GetOneToilet(new IGetOneToiletInteractor.Request(
                id));

        return Ok(DataMapper.MapToilet(response.Toilet));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPut]
    public async Task<ActionResult<Toilet>> Create(
        [FromServices] ICreateToiletInteractor interactor,
        [FromBody] CreateToiletRequest request)
    {
        var response = await interactor
            .CreateToilet(new ICreateToiletInteractor.Request(
                request.Name, request.Description));

        return CreatedAtAction(nameof(GetOne),
            new { id = response.Toilet.Id },
            DataMapper.MapToilet(response.Toilet));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost("{id:int}")]
    public async Task<ActionResult<Toilet>> Update(
        [FromServices] IUpdateToiletInteractor interactor,
        [FromRoute] int id,
        [FromBody] UpdateToiletRequest request)
    {
        var response = await interactor
            .UpdateToilet(new IUpdateToiletInteractor.Request(
                id, request.Name, request.Description));

        return CreatedAtAction(nameof(GetOne),
            new { id = response.Toilet.Id },
            DataMapper.MapToilet(response.Toilet));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(
        [FromServices] IDeleteToiletInteractor interactor,
        [FromRoute] int id)
    {
        _ = await interactor.DeleteToilet(new IDeleteToiletInteractor.Request(id));

        return Ok();
    }
}