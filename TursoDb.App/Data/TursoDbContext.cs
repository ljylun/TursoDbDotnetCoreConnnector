using Microsoft.EntityFrameworkCore;
using TursoDb.App.Models;

namespace TursoDb.App.Data;

/// <summary>
/// EF Core DbContext for the Tours database, connecting to Turso via HTTP.
/// </summary>
public class TursoDbContext : DbContext
{
    public DbSet<Tour> Tours => Set<Tour>();

    public TursoDbContext(DbContextOptions<TursoDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.ToTable("tours");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(e => e.Destination).HasColumnName("destination").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.DurationDays).HasColumnName("duration_days");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });
    }
}
