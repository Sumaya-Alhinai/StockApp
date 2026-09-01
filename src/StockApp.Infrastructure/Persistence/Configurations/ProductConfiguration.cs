using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockApp.Domain.Entities;

namespace StockApp.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Primary Key
        builder.HasKey(p => p.Id);

        // Product Name
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        // SKU
        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(64);

        // SKU must be unique
        builder.HasIndex(p => p.SKU)
            .IsUnique();

        // Category
        builder.Property(p => p.Category)
            .HasMaxLength(100);

        // Price
        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        // =====================================================
        // Concurrency Token
        // =====================================================
        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        // =====================================================
        // Product -> User
        // =====================================================
        builder.HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // =====================================================
        // Product -> StockMovement
        // =====================================================
        builder.HasMany(p => p.Movements)
            .WithOne(m => m.Product!)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}