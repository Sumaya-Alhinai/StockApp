using StockApp.Application.Products.Commands;
using StockApp.Application.Auth.Queries.Login;
using StockApp.Application.Auth.Commands.RegisterUser;
using StockApp.Domain.Entities;
using StockApp.Domain.Enums;
using Xunit;

namespace StockApp.Tests;

public class ValidatorTests : IDisposable
{
    private readonly Infrastructure.Persistence.AppDbContext _db;
    private readonly Guid _userId = Guid.NewGuid();

    public ValidatorTests()
    {
        _db = TestDbFactory.CreateFresh(out _);

        _db.Users.Add(new User
        {
            Id = _userId,
            FullName = "Existing User",
            Email = "taken@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow
        });

        _db.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = "Laptop",
            SKU = "LAP-001",
            Price = 100m,
            IsActive = true,
            StockOnHand = 0,
            CreatedByUserId = _userId,
            CreatedAt = DateTime.UtcNow
        });

        _db.SaveChanges();
    }

    [Fact]
    public void Login_validator_rejects_empty_password()
    {
        var validator = new LoginQueryValidator();
        var result = validator.Validate(new LoginQuery("a@b.com", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public void Login_validator_rejects_invalid_email_format()
    {
        var validator = new LoginQueryValidator();
        var result = validator.Validate(new LoginQuery("not-an-email", "Test1234"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task AdjustStock_validator_rejects_non_positive_quantity(int quantity)
    {
        var validator = new AdjustStockCommandValidator(_db);
        var command = new AdjustStockCommand(Guid.NewGuid(), MovementType.Out, quantity, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Quantity));
    }

    [Fact]
    public async Task AdjustStock_validator_accepts_positive_quantity()
    {
        var validator = new AdjustStockCommandValidator(_db);
        var command = new AdjustStockCommand(Guid.NewGuid(), MovementType.In, 10, "note");

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("alllower1")]
    [InlineData("ALLUPPER1")]
    [InlineData("NoDigitsHere")]
    public async Task Register_validator_rejects_weak_passwords(string password)
    {
        var validator = new RegisterUserCommandValidator(_db);

        var result = await validator.ValidateAsync(
            new RegisterUserCommand("Test User", "fresh@example.com", password));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
    }

    [Fact]
    public async Task Register_validator_rejects_duplicate_email()
    {
        var validator = new RegisterUserCommandValidator(_db);

        var result = await validator.ValidateAsync(
            new RegisterUserCommand("New User", "TAKEN@example.com", "Passw0rd"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Register_validator_accepts_unused_email()
    {
        var validator = new RegisterUserCommandValidator(_db);

        var result = await validator.ValidateAsync(
            new RegisterUserCommand("New User", "fresh@example.com", "Passw0rd"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateProduct_validator_rejects_non_positive_price(decimal price)
    {
        var validator = new CreateProductCommandValidator(_db);

        var result = await validator.ValidateAsync(
            new CreateProductCommand("Mouse", "MOU-001", price, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Price");
    }

    [Fact]
    public async Task CreateProduct_validator_rejects_duplicate_sku()
    {
        var validator = new CreateProductCommandValidator(_db);

        var result = await validator.ValidateAsync(
            new CreateProductCommand("Another laptop", "lap-001", 50m, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SKU");
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}