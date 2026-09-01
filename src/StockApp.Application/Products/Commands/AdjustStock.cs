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

        // Product ID is required
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required.");

        // Movement type must be In or Out
        RuleFor(x => x.MovementType)
            .IsInEnum()
            .WithMessage("Movement type must be In or Out.");

        // Quantity must always be greater than zero
        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");

        // Note is optional, but has a maximum length
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note cannot exceed 500 characters.");

        // --------------------------------------------------------
        // IMPORTANT:
        // This validation is only a user-friendly pre-check.
        //
        // It DOES NOT protect against concurrency/race conditions.
        // The Handler MUST check the stock again before saving.
        // --------------------------------------------------------

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
        // Therefore, don't produce a stock validation error here.
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
        // --------------------------------------------------------
        // 1. Load the product
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // 2. Calculate stock change
        // --------------------------------------------------------

        var delta = request.MovementType == MovementType.In
            ? request.Quantity
            : -request.Quantity;

        // Example:
        //
        // IN  10  -> delta = +10
        // OUT 10  -> delta = -10
        //

        var newStock = product.StockOnHand + delta;

        // --------------------------------------------------------
        // 3. AUTHORITATIVE stock check
        //
        // This check is extremely important.
        //
        // We DO NOT trust the Validator because the database
        // may have changed after validation.
        // --------------------------------------------------------

        if (newStock < 0)
        {
            throw new ConflictException(
                "INSUFFICIENT_STOCK",
                $"Cannot remove {request.Quantity}. " +
                $"Only {product.StockOnHand} in stock.");
        }

        // --------------------------------------------------------
        // 4. Update current stock
        // --------------------------------------------------------

        product.StockOnHand = newStock;

        // --------------------------------------------------------
        // 5. Create stock movement history
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // 6. Save Product + StockMovement atomically
        //
        // EF Core executes the SaveChanges operation in a
        // transaction when multiple changes are being persisted.
        //
        // If concurrency fails, neither change should be committed.
        // --------------------------------------------------------

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // ----------------------------------------------------
            // Another request modified this product after we
            // loaded it.
            //
            // This means our RowVersion/concurrency token no
            // longer matches the database.
            // ----------------------------------------------------

            throw new ConflictException(
                "CONCURRENCY_CONFLICT",
                "This product was modified by another request. " +
                "Please refresh the stock and retry.");
        }

        

        return product.StockOnHand;
    }
}