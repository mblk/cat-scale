using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Cats;

public class CreateCatInteractor
{
    public record Request(CatType Type, string Name, DateOnly DateOfBirth);
    
    private readonly IUnitOfWork _unitOfWork;

    public CreateCatInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Cat> CreateCat(Request request)
    {
        var repo = _unitOfWork.GetRepository<Cat>();

        var name = request.Name?.Trim() ?? String.Empty;
        if (String.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Invalid name");

        // TODO: Must use transaction?
        var nameAlreadyInUse = await repo
            .Query(c => c.Name == name)
            .AnyAsync();
        if (nameAlreadyInUse)
            throw new DomainValidationException("Name already in use");

        var newCat = new Cat
        {
            Type = request.Type,
            Name = name,
            DateOfBirth = request.DateOfBirth,
        };

        repo.Create(newCat);

        await _unitOfWork.SaveChangesAsync();

        return newCat;
    }
}