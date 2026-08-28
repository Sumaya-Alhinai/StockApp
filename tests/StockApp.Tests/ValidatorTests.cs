using StockApp.Application.Products.Commands;
using StockApp.Application.Auth.Queries.Login;
using StockApp.Domain.Enums;
using Xunit;

namespace StockApp.Tests;

public class ValidatorTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void Login_validator_requires_non_empty_password(string password)
    {
        var validator = new LoginQueryValidator();
        var result = validator.Validate(new LoginQuery("a@b.com", password));
        Assert.True(result.IsValid);
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
    public void AdjustStock_validator_rejects_non_positive_quantity(int quantity)
    {
        var validator = new AdjustStockCommandValidator();
        var command = new AdjustStockCommand(Guid.NewGuid(), MovementType.Out, quantity, null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Quantity));
    }

    [Fact]
    public void AdjustStock_validator_accepts_positive_quantity()
    {
        var validator = new AdjustStockCommandValidator();
        var command = new AdjustStockCommand(Guid.NewGuid(), MovementType.In, 10, "note");

        Assert.True(validator.Validate(command).IsValid);
    }
}