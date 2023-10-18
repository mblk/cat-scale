using CatScale.Application.Exceptions;
using CatScale.Application.Repository;
using CatScale.Domain.Model;

namespace CatScale.Application.UseCases.Toilets;

public interface ICreateToiletInteractor
{
    record Request(string Name, string Description);
    
    record Response(Toilet Toilet);
    
    Task<Response> CreateToilet(Request request);
}

public class CreateToiletInteractor : ICreateToiletInteractor
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateToiletInteractor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<ICreateToiletInteractor.Response> CreateToilet(ICreateToiletInteractor.Request request)
    {
        var repo = _unitOfWork.GetRepository<Toilet>();

        var nameInUse = await repo
            .Query(filter: t => t.Name == request.Name)
            .AnyAsync();

        if (nameInUse)
            throw new DomainValidationException("Name already in use");

        var newToilet = new Toilet()
        {
            Name = request.Name,
            Description = request.Description,
        };
        
        repo.Create(newToilet);

        await _unitOfWork.SaveChangesAsync();

        return new ICreateToiletInteractor.Response(newToilet);
    }
}