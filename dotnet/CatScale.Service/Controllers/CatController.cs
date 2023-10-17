using CatScale.Application.Repository;
using CatScale.Application.UseCases.Cats;
using CatScale.Service.DbModel;
using CatScale.Service.Mapper;
using CatScale.Service.Model.Cat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatScale.Service.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class CatController : ControllerBase
{
    private readonly ILogger<CatController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public CatController(ILogger<CatController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public ActionResult<IAsyncEnumerable<CatDto>> GetAll()
    {
        var cats = new GetAllCatsInteractor(_unitOfWork)
            .GetAllCats()
            .Select(DataMapper.MapCat);
        
        return Ok(cats);
    }
    
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatDto>> GetOne([FromRoute] int id)
    {
        var cat = await new GetCatInteractor(_unitOfWork)
            .GetCat(id);

        return Ok(DataMapper.MapCat(cat));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPut]
    public async Task<ActionResult<CatDto>> Create([FromBody] CreateCatRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var newCat = await new CreateCatInteractor(_unitOfWork)
            .CreateCat(new CreateCatInteractor.Request(
                DataMapper.MapCatType(request.Type),
                request.Name,
                request.DateOfBirth));
       
        return CreatedAtAction(nameof(GetOne), 
            new { id = newCat.Id }, 
            DataMapper.MapCat(newCat));
    }
    
    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpPost("{id:int}")]
    public async Task<ActionResult<CatDto>> Update([FromRoute] int id, [FromBody] UpdateCatRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingCat = await new UpdateCatInteractor(_unitOfWork)
            .UpdateCat(new UpdateCatInteractor.Request(
                id,
                DataMapper.MapCatType(request.Type),
                request.Name,
                request.DateOfBirth));
        
        return CreatedAtAction(nameof(GetOne), 
            new { id = existingCat.Id }, 
            DataMapper.MapCat(existingCat));
    }

    [Authorize(Roles = ApplicationRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await new DeleteCatInteractor(_unitOfWork)
            .DeleteCat(id);
        
        return Ok();
    }
}