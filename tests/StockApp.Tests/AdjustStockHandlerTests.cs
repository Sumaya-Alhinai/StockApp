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

    // ============================================================
    // SEED TEST DATA
    // ============================================================

    private void Seed()
    {
        _db.Users.Add(new User
        {
            Id = _userId,
            FullName = "Test User",
            Email = $"test-{_userId}@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        _db.Products.Add(new Product
        {
            Id = _productId,
            Name = "Laptop",
            SKU = $"LAP-{_productId:N}",
            Price = 100m,
            IsActive = true,
            StockOnHand = 10,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        });

        _db.SaveChanges();
    }

    // ============================================================
    // STOCK IN
    // ============================================================

    [Fact]
    public async Task Stock_in_increases_stock_and_records_movement()
    {
        // Arrange
        var handler = new AdjustStockCommandHandler(
            _db,
            new FakeCurrentUser(_userId));

        var command = new AdjustStockCommand(
            _productId,
            MovementType.In,
            5,
            "restock");

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.Equal(15, result);

        var product = await _db.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == _productId);

        Assert.Equal(15, product.StockOnHand);

        var movement = await _db.StockMovements
            .AsNoTracking()
            .SingleAsync(m => m.ProductId == _productId);

        Assert.Equal(MovementType.In, movement.MovementType);
        Assert.Equal(5, movement.Quantity);
        Assert.Equal("restock", movement.Note);
        Assert.Equal(_userId, movement.CreatedByUserId);
    }

    // ============================================================
    // STOCK OUT
    // ============================================================

    [Fact]
    public async Task Stock_out_within_available_stock_succeeds()
    {
        // Arrange
        var handler = new AdjustStockCommandHandler(
            _db,
            new FakeCurrentUser(_userId));

        var command = new AdjustStockCommand(
            _productId,
            MovementType.Out,
            4,
            null);

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.Equal(6, result);

        var product = await _db.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == _productId);

        Assert.Equal(6, product.StockOnHand);

        Assert.Equal(
            1,
            await _db.StockMovements.CountAsync(
                m => m.ProductId == _productId));
    }

    // ============================================================
    // INSUFFICIENT STOCK
    // ============================================================

    [Fact]
    public async Task Stock_out_exceeding_stock_throws_insufficient_stock()
    {
        // Arrange
        var handler = new AdjustStockCommandHandler(
            _db,
            new FakeCurrentUser(_userId));

        var command = new AdjustStockCommand(
            _productId,
            MovementType.Out,
            99,
            null);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(
                command,
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "INSUFFICIENT_STOCK",
            ex.Code);

        var product = await _db.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == _productId);

        Assert.Equal(10, product.StockOnHand);

        // No movement should be created
        Assert.Equal(
            0,
            await _db.StockMovements.CountAsync(
                m => m.ProductId == _productId));
    }

    // ============================================================
    // PRODUCT NOT FOUND
    // ============================================================

    [Fact]
    public async Task Product_not_found_throws()
    {
        // Arrange
        var handler = new AdjustStockCommandHandler(
            _db,
            new FakeCurrentUser(_userId));

        var command = new AdjustStockCommand(
            Guid.NewGuid(),
            MovementType.In,
            1,
            null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(
                command,
                CancellationToken.None));
    }

    // ============================================================
    // CONCURRENCY / RACE CONDITION
    // ============================================================

    [Fact]
    public async Task Concurrent_stock_out_allows_only_one_update()
    {
        // --------------------------------------------------------
        // IMPORTANT:
        //
        // Each request MUST use a different DbContext.
        //
        // This simulates two real HTTP requests hitting the API
        // at approximately the same time.
        // --------------------------------------------------------

        await using var dbA =
            TestDbFactory.Create(_connectionString);

        await using var dbB =
            TestDbFactory.Create(_connectionString);

        var handlerA = new AdjustStockCommandHandler(
            dbA,
            new FakeCurrentUser(_userId));

        var handlerB = new AdjustStockCommandHandler(
            dbB,
            new FakeCurrentUser(_userId));

        var commandA = new AdjustStockCommand(
            _productId,
            MovementType.Out,
            8,
            "Request A");

        var commandB = new AdjustStockCommand(
            _productId,
            MovementType.Out,
            8,
            "Request B");

        // --------------------------------------------------------
        // Load the product in BOTH contexts BEFORE either request
        // saves.
        //
        // Therefore both contexts have the same RowVersion.
        // --------------------------------------------------------

        var productA = await dbA.Products
            .FirstAsync(p => p.Id == _productId);

        var productB = await dbB.Products
            .FirstAsync(p => p.Id == _productId);

        Assert.Equal(
            productA.RowVersion,
            productB.RowVersion);

        Assert.Equal(10, productA.StockOnHand);
        Assert.Equal(10, productB.StockOnHand);

        // --------------------------------------------------------
        // Execute both updates.
        //
        // Only one should successfully update the row.
        // The other should receive DbUpdateConcurrencyException.
        // --------------------------------------------------------

        var taskA = handlerA.Handle(
            commandA,
            CancellationToken.None);

        var taskB = handlerB.Handle(
            commandB,
            CancellationToken.None);

        var results = await Task.WhenAll(
            CaptureResult(taskA),
            CaptureResult(taskB));

        // --------------------------------------------------------
        // One request must succeed.
        // One request must fail because of concurrency.
        // --------------------------------------------------------

        var successCount = results.Count(r => r.Success);
        var concurrencyCount = results.Count(r =>
            r.Exception is ConflictException conflict &&
            conflict.Code == "CONCURRENCY_CONFLICT");

        Assert.Equal(1, successCount);
        Assert.Equal(1, concurrencyCount);

        // --------------------------------------------------------
        // Verify final database state.
        // --------------------------------------------------------

        await using var verificationDb =
            TestDbFactory.Create(_connectionString);

        var finalProduct = await verificationDb.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == _productId);

        // Only one OUT 8 was allowed.
        Assert.Equal(2, finalProduct.StockOnHand);

        // Only one movement should have been recorded.
        Assert.Equal(
            1,
            await verificationDb.StockMovements.CountAsync(
                m => m.ProductId == _productId));
    }

    // ============================================================
    // HELPER FOR CONCURRENCY TEST
    // ============================================================

    private static async Task<TestResult> CaptureResult(
        Task<int> task)
    {
        try
        {
            var result = await task;

            return new TestResult(
                true,
                result,
                null);
        }
        catch (Exception ex)
        {
            return new TestResult(
                false,
                null,
                ex);
        }
    }

    private sealed record TestResult(
        bool Success,
        int? Result,
        Exception? Exception);

    // ============================================================
    // CLEANUP
    // ============================================================

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}