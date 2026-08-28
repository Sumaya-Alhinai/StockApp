using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Common.Interfaces;
using StockApp.Domain.Entities;

namespace StockApp.Application.Products.Commands;

public record CreateProductCommand(
    string Name,
    string SKU,
    decimal Price,
    string? Category) : IRequest<Guid>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly IAppDbContext _db;

    public CreateProductCommandValidator(IAppDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(64)
            .MustAsync(BeUniqueSku).WithMessage("This SKU is already in use.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Category).MaximumLength(100);
    }

    private async Task<bool> BeUniqueSku(string sku, CancellationToken ct)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return !await _db.Products.AnyAsync(p => p.SKU == normalized, ct);
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateProductCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            SKU = request.SKU.Trim().ToUpperInvariant(),
            Price = request.Price,
            Category = request.Category?.Trim(),
            IsActive = true,
            StockOnHand = 0,
            CreatedByUserId = _currentUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("DUPLICATE_SKU", "This SKU is already in use.");
        }

        return product.Id;
    }
}