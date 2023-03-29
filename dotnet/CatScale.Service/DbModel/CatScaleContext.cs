using CatScale.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace CatScale.Service.DbModel;

public class CatScaleContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public DbSet<Toilet> Toilets { get; set; } = null!;
    public DbSet<Cat> Cats { get; set; } = null!;
    public DbSet<CatWeight> CatWeights { get; set; } = null!;
    public DbSet<Measurement> Measurements { get; set; } = null!;
    public DbSet<Cleaning> Cleanings { get; set; } = null!;

    public DbSet<UserApiKey> UserApiKeys { get; set; } = null!;

    public CatScaleContext(DbContextOptions<CatScaleContext> contextOptions)
        :base(contextOptions) { }
}
