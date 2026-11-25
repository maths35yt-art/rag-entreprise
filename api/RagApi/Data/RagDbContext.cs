using Microsoft.EntityFrameworkCore;

namespace RagApi.Data;

public class RagDbContext : DbContext
{
    public RagDbContext(DbContextOptions<RagDbContext> options) : base(options)
    {
    }

    public DbSet<ExchangeEntity> Exchanges => Set<ExchangeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExchangeEntity>(entity =>
        {
            entity.HasKey(exchange => exchange.Id);
            entity.Property(exchange => exchange.Question).IsRequired().HasMaxLength(2000);
            entity.Property(exchange => exchange.Answer).IsRequired();
            entity.Property(exchange => exchange.PassagesJson).IsRequired();
        });
    }
}