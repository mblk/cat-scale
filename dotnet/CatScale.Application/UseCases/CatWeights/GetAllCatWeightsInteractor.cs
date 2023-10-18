using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.CatWeights;

public class GetAllCatWeightsInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllCatWeightsInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IAsyncEnumerable<CatWeight>> GetAllCatWeights(int catId)
    {
        var cat = await _unitOfWork.GetRepository<Cat>()
            .Query(filter: c => c.Id == catId, includes: new[] { nameof(Cat.Weights) })
            .SingleOrDefaultAsync();

        if (cat is null)
            throw new EntityNotFoundException($"Cat {catId} not found");
        
        return _unitOfWork.GetRepository<CatWeight>()
            .Query(filter: cw => cw.CatId == catId,
                order: x => x.OrderBy(cw => cw.Id));
    }
}