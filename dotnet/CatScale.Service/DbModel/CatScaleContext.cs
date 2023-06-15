using CatScale.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CatScale.Service.DbModel;

public class CatScaleContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public DbSet<Toilet> Toilets { get; set; } = null!;
    public DbSet<Cat> Cats { get; set; } = null!;
    public DbSet<CatWeight> CatWeights { get; set; } = null!;
    public DbSet<ScaleEvent> ScaleEvents { get; set; } = null!;
    public DbSet<StablePhase> StablePhases { get; set; } = null!;
    public DbSet<Measurement> Measurements { get; set; } = null!;
    public DbSet<Cleaning> Cleanings { get; set; } = null!;
    public DbSet<Food> Foods { get; set; } = null!;
    public DbSet<Feeding> Feedings { get; set; } = null!;

    public DbSet<UserApiKey> UserApiKeys { get; set; } = null!;

    public CatScaleContext(DbContextOptions<CatScaleContext> contextOptions)
        :base(contextOptions) { }
}
