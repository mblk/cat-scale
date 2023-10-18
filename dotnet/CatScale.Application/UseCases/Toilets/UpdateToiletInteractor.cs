using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Toilets;

public interface IUpdateToiletInteractor
{
    record Request(int Id, string Name, string Description);

    record Response(Toilet Toilet);

    Task<Response> UpdateToilet(Request request);
}

public class UpdateToiletInteractor : IUpdateToiletInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateToiletInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IUpdateToiletInteractor.Response> UpdateToilet(IUpdateToiletInteractor.Request request)
    {
        var repo = _unitOfWork.GetRepository<Toilet>();

        var toilet = await repo
            .Query(filter: t => t.Id == request.Id)
            .SingleOrDefaultAsync();
        if (toilet is null)
            throw new EntityNotFoundException("Toilet not found");

        var nameAlreadyInUse = await repo
            .Query(filter: t => t.Name == request.Name && t.Id != request.Id)
            .AnyAsync();
        if (nameAlreadyInUse)
            throw new DomainValidationException("Name already in use");
        
        toilet.Name = request.Name;
        toilet.Description = request.Description;
        
        repo.Update(toilet);

        await _unitOfWork.SaveChangesAsync();

        return new IUpdateToiletInteractor.Response(toilet);
    }
}