using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Products.Commands;
using StockApp.Domain.Entities;
using StockApp.Domain.Enums;
using Xunit;

namespace StockApp.Tests;

public class AdjustStockHandlerTests : IDisposable
{
    private readonly Infrastructure.Persistence.AppDbContext _db;
    private readonly string _connectionString;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    public AdjustStockHandlerTests()
    {
        _db = TestDbFactory.CreateFresh(out _connectionString);
        Seed();
    }

    private void Seed()
    {
        _db.Users.Add(new User
        {
            Id = _userId,
            FullName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        _db.Products.Add(new Product
        {
            Id = _productId,
            Name = "Laptop",
            SKU = "LAP-001",
            Price = 100m,
            IsActive = true,
            StockOnHand = 10,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        });

        _db.SaveChanges();
    }

    [Fact]
    public async Task Stock_in_increases_stock_and_records_movement()
    {
        var handler = new AdjustStockCommandHandler(_db, new FakeCurrentUser(_userId));

        var result = await handler.Handle(
            new AdjustStockCommand(_productId, MovementType.In, 5, "restock"),
            CancellationToken.None);

        Assert.Equal(15, result);
        Assert.Equal(1, await _db.StockMovements.CountAsync(m => m.ProductId == _productId));
    }

    [Fact]
    public async Task Stock_out_within_available_stock_succeeds()
    {
        var handler = new AdjustStockCommandHandler(_db, new FakeCurrentUser(_userId));

        var result = await handler.Handle(
            new AdjustStockCommand(_productId, MovementType.Out, 4, null),
            CancellationToken.None);

        Assert.Equal(6, result);
    }

    [Fact]
    public async Task Stock_out_exceeding_stock_throws_insufficient_stock()
    {
        var handler = new AdjustStockCommandHandler(_db, new FakeCurrentUser(_userId));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(
                new AdjustStockCommand(_productId, MovementType.Out, 99, null),
                CancellationToken.None));

        Assert.Equal("INSUFFICIENT_STOCK", ex.Code);

        var product = await _db.Products.AsNoTracking()
            .FirstAsync(p => p.Id == _productId);
        Assert.Equal(10, product.StockOnHand);
    }

    [Fact]
    public async Task Product_not_found_throws()
    {
        var handler = new AdjustStockCommandHandler(_db, new FakeCurrentUser(_userId));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new AdjustStockCommand(Guid.NewGuid(), MovementType.In, 1, null),
                CancellationToken.None));
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}