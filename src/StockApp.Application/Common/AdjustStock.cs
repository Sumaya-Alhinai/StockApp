using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Common.Interfaces;
using StockApp.Domain.Entities;
using StockApp.Domain.Enums;

namespace StockApp.Application.Products.Commands;

public record AdjustStockCommand(
    Guid ProductId,
    MovementType MovementType,
    int Quantity,
    string? Note) : IRequest<int>;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.MovementType)
            .IsInEnum().WithMessage("Movement type must be In or Out.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AdjustStockCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException("Product", request.ProductId);

        var delta = request.MovementType == MovementType.In
            ? request.Quantity
            : -request.Quantity;

        var newStock = product.StockOnHand + delta;

        if (newStock < 0)
            throw new ConflictException(
                "INSUFFICIENT_STOCK",
                $"Cannot remove {request.Quantity}. Only {product.StockOnHand} in stock.");

        product.StockOnHand = newStock;

        _db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            MovementType = request.MovementType,
            Quantity = request.Quantity,
            Note = request.Note?.Trim(),
            CreatedByUserId = _currentUser.Id,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "CONCURRENCY_CONFLICT",
                "This product was modified by another request. Please retry.");
        }

        return product.StockOnHand;
    }
}