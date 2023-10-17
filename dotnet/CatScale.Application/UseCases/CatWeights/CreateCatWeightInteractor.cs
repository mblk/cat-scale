using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.CatWeights;

public class CreateCatWeightInteractor
{
    public record Request(int CatId, DateTimeOffset Timestamp, double Weight);
    
    private readonly IUnitOfWork _unitOfWork;

    public CreateCatWeightInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CatWeight> CreateCatWeight(Request request)
    {
        var catRepo = _unitOfWork.GetRepository<Cat>();
        var catWeightRepo = _unitOfWork.GetRepository<CatWeight>();

        var cat = await catRepo
            .Query(filter: c => c.Id == request.CatId)
            .SingleOrDefaultAsync();

        if (cat is null)
            throw new EntityNotFoundException($"Cat {request.CatId} not found");

        var newCatWeight = new CatWeight()
        {
            CatId = cat.Id,
            Timestamp = request.Timestamp.ToUniversalTime(),
            Weight = request.Weight,
        };

        catWeightRepo.Create(newCatWeight);
        
        await _unitOfWork.SaveChangesAsync();

        return newCatWeight;
    }
}