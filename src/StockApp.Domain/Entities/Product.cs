namespace StockApp.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; } = true;

    public int StockOnHand { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}