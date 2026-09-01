using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Common.Interfaces;
using StockApp.Domain.Entities;
using StockApp.Domain.Enums;

namespace StockApp.Application.Products.Commands;

// ============================================================
// COMMAND
// ============================================================

public record AdjustStockCommand(
    Guid ProductId,
    MovementType MovementType,
    int Quantity,
    string? Note) : IRequest<int>;


// ============================================================
// VALIDATOR
// ============================================================

public class AdjustStockCommandValidator
    : AbstractValidator<AdjustStockCommand>
{
    private readonly IAppDbContext _db;

    public AdjustStockCommandValidator(IAppDbContext db)
    {
        _db = db;

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        RuleFor(x => x.MovementType)
            .IsInEnum()
            .WithMessage("Movement type must be In or Out.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note cannot exceed 500 characters.");

        // Early stock validation.
        // The Handler performs the authoritative check again.
        RuleFor(x => x)
            .MustAsync(NotExceedStockOnHand)
            .WithName("Quantity")
            .WithMessage("Stock-out quantity exceeds stock on hand.")
            .When(x =>
                x.MovementType == MovementType.Out &&
                x.Quantity > 0);
    }

    private async Task<bool> NotExceedStockOnHand(
        AdjustStockCommand command,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == command.ProductId,
                cancellationToken);

        // Product existence is handled by the Handler.
        if (product is null)
            return true;

        return command.Quantity <= product.StockOnHand;
    }
}


// ============================================================
// HANDLER
// ============================================================

public class AdjustStockCommandHandler
    : IRequestHandler<AdjustStockCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AdjustStockCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(
        AdjustStockCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load product
        var product = await _db.Products
            .FirstOrDefaultAsync(
                p => p.Id == request.ProductId,
                cancellationToken);

        if (product is null)
        {
            throw new NotFoundException(
                "Product",
                request.ProductId);
        }

        // 2. Calculate stock change
        var delta = request.MovementType == MovementType.In
            ? request.Quantity
            : -request.Quantity;

        var newStock = product.StockOnHand + delta;

        // 3. Authoritative stock check
        if (newStock < 0)
        {
            throw new ConflictException(
                "INSUFFICIENT_STOCK",
                $"Cannot remove {request.Quantity}. " +
                $"Only {product.StockOnHand} in stock.");
        }

        // 4. Update product stock
        product.StockOnHand = newStock;

        // 5. Create movement history
        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            MovementType = request.MovementType,
            Quantity = request.Quantity,
            Note = string.IsNullOrWhiteSpace(request.Note)
                ? null
                : request.Note.Trim(),
            CreatedByUserId = _currentUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        _db.StockMovements.Add(movement);

        // 6. Save changes
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "CONCURRENCY_CONFLICT",
                "This product was modified by another request. " +
                "Please refresh the stock and retry.");
        }

        // 7. Return updated stock
        return product.StockOnHand;
    }
}