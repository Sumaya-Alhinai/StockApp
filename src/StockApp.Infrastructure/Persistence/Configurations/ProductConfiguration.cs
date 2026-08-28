using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockApp.Domain.Entities;

namespace StockApp.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.SKU).IsRequired().HasMaxLength(64);
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.Price).HasPrecision(18, 2);

        builder.HasIndex(p => p.SKU).IsUnique();

        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasOne(p => p.CreatedByUser)
               .WithMany()
               .HasForeignKey(p => p.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Movements)
               .WithOne(m => m.Product!)
               .HasForeignKey(m => m.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}