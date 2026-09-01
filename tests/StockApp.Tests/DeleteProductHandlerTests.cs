using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Products.Commands;
using StockApp.Domain.Entities;
using StockApp.Domain.Enums;
using Xunit;

namespace StockApp.Tests;

public class DeleteProductHandlerTests : IDisposable
{
    private readonly Infrastructure.Persistence.AppDbContext _db;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _cleanId = Guid.NewGuid();
    private readonly Guid _usedId = Guid.NewGuid();

    public DeleteProductHandlerTests()
    {
        _db = TestDbFactory.CreateFresh(out _);

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
            Id = _cleanId,
            Name = "Never Stocked",
            SKU = "CLEAN-001",
            Price = 10m,
            IsActive = true,
            StockOnHand = 0,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        });

        _db.Products.Add(new Product
        {
            Id = _usedId,
            Name = "Has History",
            SKU = "USED-001",
            Price = 20m,
            IsActive = true,
            StockOnHand = 5,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        });

        _db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = _usedId,
            MovementType = MovementType.In,
            Quantity = 5,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        });

        _db.SaveChanges();
    }

    [Fact]
    public async Task Product_without_movements_is_deleted()
    {
        var handler = new DeleteProductCommandHandler(_db);

        await handler.Handle(new DeleteProductCommand(_cleanId), CancellationToken.None);

        Assert.False(await _db.Products.AnyAsync(p => p.Id == _cleanId));
    }

    [Fact]
    public async Task Product_with_movements_is_blocked_and_deactivated()
    {
        var handler = new DeleteProductCommandHandler(_db);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new DeleteProductCommand(_usedId), CancellationToken.None));

        Assert.Equal("DELETE_BLOCKED", ex.Code);

        var product = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == _usedId);
        Assert.False(product.IsActive);
    }

    [Fact]
    public async Task Deactivate_sets_product_inactive()
    {
        var handler = new DeactivateProductCommandHandler(_db);

        await handler.Handle(new DeactivateProductCommand(_usedId), CancellationToken.None);

        var product = await _db.Products.AsNoTracking().FirstAsync(p => p.Id == _usedId);
        Assert.False(product.IsActive);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}