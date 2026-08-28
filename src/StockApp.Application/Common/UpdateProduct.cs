using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Common.Interfaces;

namespace StockApp.Application.Products.Commands;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string SKU,
    decimal Price,
    string? Category) : IRequest<Unit>;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private readonly IAppDbContext _db;

    public UpdateProductCommandValidator(IAppDbContext db)
    {
        _db = db;

        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(64);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Category).MaximumLength(100);

        RuleFor(x => x)
            .MustAsync(BeUniqueSku).WithName("SKU")
            .WithMessage("This SKU is already in use.");
    }

    private async Task<bool> BeUniqueSku(UpdateProductCommand cmd, CancellationToken ct)
    {
        var normalized = cmd.SKU.Trim().ToUpperInvariant();
        return !await _db.Products
            .AnyAsync(p => p.SKU == normalized && p.Id != cmd.Id, ct);
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Unit>
{
    private readonly IAppDbContext _db;

    public UpdateProductCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("Product", request.Id);

        product.Name = request.Name.Trim();
        product.SKU = request.SKU.Trim().ToUpperInvariant();
        product.Price = request.Price;
        product.Category = request.Category?.Trim();

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("DUPLICATE_SKU", "This SKU is already in use.");
        }

        return Unit.Value;
    }
}