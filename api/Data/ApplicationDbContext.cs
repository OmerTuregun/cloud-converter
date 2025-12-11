using CloudConverter.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CloudConverter.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Video> Videos => Set<Video>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>(entity =>
        {
            entity.Property(v => v.FileName).IsRequired();
            entity.Property(v => v.Status).IsRequired();
            entity.Property(v => v.S3Url).IsRequired();
            entity.Property(v => v.Tags);
            entity.Property(v => v.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .ValueGeneratedOnAdd();
        });
    }
}

