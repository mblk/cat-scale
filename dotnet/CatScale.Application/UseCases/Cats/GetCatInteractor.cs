using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Cats;

public class GetCatInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCatInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Cat> GetCat(int id)
    {
        var cat = await _unitOfWork
            .GetRepository<Cat>()
            .Query(c => c.Id == id)
            .SingleOrDefaultAsync();

        if (cat is null)
            throw new EntityNotFoundException("Cat not found");

        return cat;
    }
}