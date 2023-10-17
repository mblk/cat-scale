using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Cats;

public class UpdateCatInteractor
{
    public record Request(int Id, CatType Type, string Name, DateOnly DateOfBirth);
    
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCatInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Cat> UpdateCat(Request request)
    {
        var repo = _unitOfWork.GetRepository<Cat>();

        var existingCat = await repo
            .Query(c => c.Id == request.Id)
            .SingleOrDefaultAsync();

        if (existingCat is null)
            throw new EntityNotFoundException("Cat not found");

        var name = request.Name?.Trim() ?? String.Empty;
        if (String.IsNullOrWhiteSpace(name))
            throw new DomainValidationException("Invalid name");

        // TODO: Must use transaction?
        var newNameAlreadyInUse = await repo
            .Query(c => c.Name == name && c.Id != existingCat.Id)
            .AnyAsync();
        
        if (newNameAlreadyInUse)
            throw new DomainValidationException("Name already in use");

        existingCat.Type = request.Type;
        existingCat.Name = name;
        existingCat.DateOfBirth = request.DateOfBirth;

        repo.Update(existingCat);

        await _unitOfWork.SaveChangesAsync();

        return existingCat;
    }
}