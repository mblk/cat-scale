using CatScale.Service.DbModel;
using Microsoft.EntityFrameworkCore;

namespace CatScale.Service.Services;

public interface IApiKeyService
{
    Task<IEnumerable<UserApiKey>> GetApiKeys(ApplicationUser user);
    Task<UserApiKey> CreateApiKey(ApplicationUser user, DateTime? expirationDate);
    Task DeleteApiKey(ApplicationUser user, int apiKeyId);
}

public class ApiKeyService : IApiKeyService
{
    private readonly CatScaleContext _context;

    public ApiKeyService(CatScaleContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<UserApiKey>> GetApiKeys(ApplicationUser user)
    {
        var apiKeys = await _context.UserApiKeys
            .AsNoTracking()
            .Where(k => k.User == user)
            .ToArrayAsync();

        return apiKeys;
    }
    
    public async Task<UserApiKey> CreateApiKey(ApplicationUser user, DateTime? expirationDate)
    {
        var newApiKey = new UserApiKey
        {
            User = user,
            Value = GenerateApiKeyValue()
        };

        // TODO store/validate expiration date
        
        await _context.UserApiKeys.AddAsync(newApiKey);
        await _context.SaveChangesAsync();

        return newApiKey;
    }

    private static string GenerateApiKeyValue()
    {
        return $"{Guid.NewGuid().ToString()}-{Guid.NewGuid().ToString()}"; // TODO
    }

    public async Task DeleteApiKey(ApplicationUser user, int apiKeyId)
    {
        var keysToDelete = _context.UserApiKeys
            .Where(k => k.User == user && k.Id == apiKeyId);

        _context.UserApiKeys.RemoveRange(keysToDelete);
        await _context.SaveChangesAsync();
    }
}