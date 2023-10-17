using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.CatWeights;

public class GetCatWeightInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCatWeightInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CatWeight> GetCatWeight(int catWeightId)
    {
        var catWeight = await _unitOfWork.GetRepository<CatWeight>()
            .Query(filter: cw => cw.Id == catWeightId)
            .SingleOrDefaultAsync();

        if (catWeight is null)
            throw new EntityNotFoundException($"Cat weight {catWeightId} not found");

        return catWeight;
    }
}