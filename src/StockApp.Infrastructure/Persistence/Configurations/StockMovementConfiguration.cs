using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockApp.Domain.Entities;

namespace StockApp.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementType).IsRequired().HasConversion<int>();
        builder.Property(m => m.Quantity).IsRequired();
        builder.Property(m => m.Note).HasMaxLength(500);

        builder.HasIndex(m => new { m.ProductId, m.CreatedAt });
    }
}