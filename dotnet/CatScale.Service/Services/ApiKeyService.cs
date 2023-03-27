using CatScale.Service.DbModel;
using CatScale.Service.RestModel;
using Microsoft.AspNetCore.Identity;

namespace CatScale.Service.Services;

public interface IApiKeyService
{
    Task<AuthenticationResponse> CreateApiKey(IdentityUser user);
}

public class ApiKeyService : IApiKeyService
{
    private readonly CatScaleContext _context;

    public ApiKeyService(CatScaleContext context)
    {
        _context = context;
    }

    public async Task<AuthenticationResponse> CreateApiKey(IdentityUser user)
    {
        var newApiKey = new UserApiKey
        {
            User = user,
            Value = GenerateApiKeyValue()
        };

        await _context.UserApiKeys.AddAsync(newApiKey);
        await _context.SaveChangesAsync();

        return new AuthenticationResponse()
        {
            Expiration = DateTime.MaxValue,
            Token = newApiKey.Value,
        };
    }

    private static string GenerateApiKeyValue()
    {
        return $"{Guid.NewGuid().ToString()}-{Guid.NewGuid().ToString()}";
    }
}