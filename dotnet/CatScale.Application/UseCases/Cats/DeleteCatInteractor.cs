using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Cats;

public class DeleteCatInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCatInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task DeleteCat(int id)
    {
        var repo = _unitOfWork.GetRepository<Cat>();

        var existingCat = await repo
            .Query(c => c.Id == id, includes: new [] { nameof(Cat.Measurements) })
            .SingleOrDefaultAsync();

        if (existingCat is null)
            throw new EntityNotFoundException("Cat not found");

        // Don't delete 'production' data by accident.
        var numMeasurements = existingCat.Measurements.Count;
        if (numMeasurements > 10)
            throw new DomainValidationException("Not deleting cat because it has too many measurements");

        repo.Delete(existingCat);

        await _unitOfWork.SaveChangesAsync();
    }
}