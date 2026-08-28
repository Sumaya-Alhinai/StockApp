using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Products.Commands;
using StockApp.Domain.Entities;
using StockApp.Domain.Enums;
using Xunit;

namespace StockApp.Tests;

public class ConcurrentStockAdjustmentTests : IDisposable
{
    private readonly string _connectionString;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    public ConcurrentStockAdjustmentTests()
    {
        using var db = TestDbFactory.CreateFresh(out _connectionString);

        db.Users.Add(new User
        {
            Id = _userId,
            FullName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        db.Products.Add(new Product
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

        db.SaveChanges();
    }

    [Fact]
    public async Task Two_concurrent_stock_outs_cannot_both_succeed()
    {
        using var dbA = TestDbFactory.Create(_connectionString);
        using var dbB = TestDbFactory.Create(_connectionString);

        var productA = await dbA.Products.FirstAsync(p => p.Id == _productId);
        var productB = await dbB.Products.FirstAsync(p => p.Id == _productId);

        Assert.Equal(10, productA.StockOnHand);
        Assert.Equal(10, productB.StockOnHand);

        var handlerA = new AdjustStockCommandHandler(dbA, new FakeCurrentUser(_userId));
        var handlerB = new AdjustStockCommandHandler(dbB, new FakeCurrentUser(_userId));

        var command = new AdjustStockCommand(_productId, MovementType.Out, 8, null);

        var errorA = await Record.ExceptionAsync(() =>
            handlerA.Handle(command, CancellationToken.None));

        var errorB = await Record.ExceptionAsync(() =>
            handlerB.Handle(command, CancellationToken.None));

        var outcomes = new[] { errorA, errorB };

        Assert.Single(outcomes, o => o is null);

        var failure = Assert.Single(outcomes, o => o is not null);
        var conflict = Assert.IsType<ConflictException>(failure);
        Assert.Equal("CONCURRENCY_CONFLICT", conflict.Code);

        using var verify = TestDbFactory.Create(_connectionString);
        var product = await verify.Products.AsNoTracking()
            .FirstAsync(p => p.Id == _productId);

        Assert.Equal(2, product.StockOnHand);
        Assert.True(product.StockOnHand >= 0);

        Assert.Equal(1, await verify.StockMovements
            .CountAsync(m => m.ProductId == _productId));
    }

    public void Dispose()
    {
        using var db = TestDbFactory.Create(_connectionString);
        db.Database.EnsureDeleted();
    }
}