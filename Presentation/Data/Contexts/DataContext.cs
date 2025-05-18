using Microsoft.EntityFrameworkCore;
using Presentation.Data.Entities;

namespace Presentation.Data.Contexts;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<VerificationCodeEntity> VerificationCodes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<VerificationCodeEntity>()
            .HasIndex(v => v.Email)
            .IsUnique(false);
    }
}
