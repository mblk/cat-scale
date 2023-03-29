using System.Security.Claims;
using System.Text.Encodings.Web;
using CatScale.Service.DbModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CatScale.Service.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string API_KEY_HEADER = "ApiKey";

    private readonly CatScaleContext _context;

    public ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, CatScaleContext context
    ) : base(options, logger, encoder, clock)
    {
        _context = context;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(API_KEY_HEADER))
            return AuthenticateResult.Fail("Header Not Found.");

        string? apiKeyToValidate = Request.Headers[API_KEY_HEADER];
        if (String.IsNullOrWhiteSpace((apiKeyToValidate)))
            return AuthenticateResult.Fail("Invalid key."); 

        var apiKey = await _context.UserApiKeys
            .Include(uak => uak.User)
            .SingleOrDefaultAsync(uak => uak.Value == apiKeyToValidate);

        if (apiKey is null)
            return AuthenticateResult.Fail("Invalid key.");

        return AuthenticateResult.Success(CreateTicket(apiKey.User));
    }

    private AuthenticationTicket CreateTicket(ApplicationUser user)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user?.UserName ?? String.Empty),
            new Claim(ClaimTypes.Email, user?.Email ?? String.Empty)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return ticket;
    }
}
