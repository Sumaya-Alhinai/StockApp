using Microsoft.EntityFrameworkCore;
using StockApp.Application.Products.Queries;
using StockApp.Domain.Entities;
using StockApp.Infrastructure.Persistence;
using Xunit;

namespace StockApp.Tests;

public class ListProductsQueryTests : IDisposable
{
private readonly AppDbContext _db;
private readonly string _connectionString;
private readonly Guid _userId = Guid.NewGuid();


public ListProductsQueryTests()
{
    _db = TestDbFactory.CreateFresh(out _connectionString);
    SeedProducts();
}

private void SeedProducts()
{
    _db.Users.Add(new User
    {
        Id = _userId,
        FullName = "Test User",
        Email = $"test-{Guid.NewGuid()}@example.com",
        PasswordHash = "hash",
        CreatedAt = DateTime.UtcNow
    });

    for (int i = 1; i <= 25; i++)
    {
        _db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = $"Product {i}",
            SKU = $"SKU-{i:D3}",
            Price = 100 + i,
            Category = i % 2 == 0 ? "Electronics" : "Office",
            IsActive = true,
            StockOnHand = i * 10,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow.AddMinutes(-i)
        });
    }

    _db.SaveChanges();
}

[Fact]
public async Task Page_1_returns_first_10_products()
{
    var handler = new ListProductsQueryHandler(_db);

    var result = await handler.Handle(
        new ListProductsQuery(
            Search: null,
            PageNumber: 1,
            PageSize: 10),
        CancellationToken.None);

    Assert.Equal(10, result.Items.Count);
    Assert.Equal(1, result.PageNumber);
    Assert.Equal(10, result.PageSize);
    Assert.Equal(25, result.TotalCount);
    Assert.Equal(3, result.TotalPages);
}

[Fact]
public async Task Page_2_returns_next_10_products()
{
    var handler = new ListProductsQueryHandler(_db);

    var result = await handler.Handle(
        new ListProductsQuery(
            Search: null,
            PageNumber: 2,
            PageSize: 10),
        CancellationToken.None);

    Assert.Equal(10, result.Items.Count);
    Assert.Equal(2, result.PageNumber);
    Assert.Equal(10, result.PageSize);
    Assert.Equal(25, result.TotalCount);
    Assert.Equal(3, result.TotalPages);
}

[Fact]
public async Task Last_page_returns_remaining_products()
{
    var handler = new ListProductsQueryHandler(_db);

    var result = await handler.Handle(
        new ListProductsQuery(
            Search: null,
            PageNumber: 3,
            PageSize: 10),
        CancellationToken.None);

    Assert.Equal(5, result.Items.Count);
    Assert.Equal(3, result.PageNumber);
    Assert.Equal(10, result.PageSize);
    Assert.Equal(25, result.TotalCount);
    Assert.Equal(3, result.TotalPages);
}

[Fact]
public async Task Page_size_is_respected()
{
    var handler = new ListProductsQueryHandler(_db);

    var result = await handler.Handle(
        new ListProductsQuery(
            Search: null,
            PageNumber: 1,
            PageSize: 5),
        CancellationToken.None);

    Assert.Equal(5, result.Items.Count);
    Assert.Equal(5, result.PageSize);
    Assert.Equal(25, result.TotalCount);
    Assert.Equal(5, result.TotalPages);
}

[Fact]
public async Task Search_works_with_pagination()
{
    var handler = new ListProductsQueryHandler(_db);

    var result = await handler.Handle(
        new ListProductsQuery(
            Search: "Electronics",
            PageNumber: 1,
            PageSize: 5),
        CancellationToken.None);

    Assert.Equal(5, result.Items.Count);
    Assert.Equal(12, result.TotalCount);
    Assert.Equal(3, result.TotalPages);

    Assert.All(
        result.Items,
        product => Assert.Equal(
            "Electronics",
            product.Category));
}

[Fact]
public async Task Search_by_sku_returns_matching_product()
{
    var handler = new ListProductsQueryHandler(_db);

    var result = await handler.Handle(
        new ListProductsQuery(
            Search: "SKU-005",
            PageNumber: 1,
            PageSize: 10),
        CancellationToken.None);

    Assert.Single(result.Items);

    Assert.Equal(
        "SKU-005",
        result.Items[0].SKU);

    Assert.Equal(1, result.TotalCount);
    Assert.Equal(1, result.TotalPages);
}

[Fact]
public async Task Search_with_no_match_returns_empty_result()
{
    var handler = new ListProductsQueryHandler(_db);

    var result = await handler.Handle(
        new ListProductsQuery(
            Search: "DoesNotExist",
            PageNumber: 1,
            PageSize: 10),
        CancellationToken.None);

    Assert.Empty(result.Items);
    Assert.Equal(0, result.TotalCount);
    Assert.Equal(0, result.TotalPages);
}

public void Dispose()
{
    _db.Database.EnsureDeleted();
    _db.Dispose();
}


}
