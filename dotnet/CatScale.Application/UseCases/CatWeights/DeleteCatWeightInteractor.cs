using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.CatWeights;

public class DeleteCatWeightInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCatWeightInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task DeleteCatWeight(int catWeightId)
    {
        var catWeightRepo = _unitOfWork.GetRepository<CatWeight>();
        var catRepo = _unitOfWork.GetRepository<Cat>();

        var catWeight = await catWeightRepo
            .Query(cw => cw.Id == catWeightId)
            .SingleOrDefaultAsync();

        if (catWeight is null)
            throw new EntityNotFoundException($"Cat weight {catWeightId} not found");

        var cat = await catRepo
            .Query(filter: c => c.Id == catWeight.CatId, includes: nameof(Cat.Weights))
            .SingleAsync();

        var hasMoreWeights = cat.Weights.Any(cw => cw.Id != catWeight.Id);

        if (!hasMoreWeights)
            throw new DomainValidationException("Mustn't delete last remaining cat weight");
        
        catWeightRepo.Delete(catWeight);

        await _unitOfWork.SaveChangesAsync();
    }
}