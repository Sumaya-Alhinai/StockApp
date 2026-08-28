using StockApp.Domain.Enums;

namespace StockApp.Domain.Entities;

public class StockMovement
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}