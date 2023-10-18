using CatScale.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CatScale.Service.DbModel;

public class CatScaleDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
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

    public CatScaleDbContext(DbContextOptions<CatScaleDbContext> contextOptions)
        : base(contextOptions)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // https://blog.dangl.me/archive/handling-datetimeoffset-in-sqlite-with-entity-framework-core/
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
            // here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
            // To work around this, when the Sqlite database provider is used, all model properties of type DateTimeOffset
            // use the DateTimeOffsetToBinaryConverter
            // Based on: https://github.com/aspnet/EntityFrameworkCore/issues/10784#issuecomment-415769754
            // This only supports millisecond precision, but should be sufficient for most use cases.
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType
                    .GetProperties()
                    .Where(p => p.PropertyType == typeof(DateTimeOffset)
                                || p.PropertyType == typeof(DateTimeOffset?));
                foreach (var property in properties)
                {
                    builder
                        .Entity(entityType.Name)
                        .Property(property.Name)
                        .HasConversion(new DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}