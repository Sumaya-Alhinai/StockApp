using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Common.Interfaces;

namespace StockApp.Application.Products.Commands;

// ============================================================
// DELETE PRODUCT
// ============================================================
// A product with recorded stock movements is part of the audit
// trail and cannot be removed. The command refuses the request
// and leaves the database untouched — deactivation is a separate,
// explicit decision made through DeactivateProductCommand.
// ============================================================

public record DeleteProductCommand(Guid Id) : IRequest<Unit>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IAppDbContext _db;

    public DeleteProductCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("Product", request.Id);

        var hasMovements = await _db.StockMovements
            .AnyAsync(m => m.ProductId == request.Id, ct);

        if (hasMovements)
        {
            // No state change before throwing: a failed request must
            // leave the database exactly as it found it.
            throw new ConflictException(
                "DELETE_BLOCKED",
                "This product has stock movement history and cannot be deleted. Deactivate it instead.");
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}


// ============================================================
// DEACTIVATE PRODUCT
// ============================================================
// The supported alternative to deletion. Keeps the product and its
// movement history intact while removing it from active use.
// ============================================================

public record DeactivateProductCommand(Guid Id) : IRequest<Unit>;

public class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand, Unit>
{
    private readonly IAppDbContext _db;

    public DeactivateProductCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(DeactivateProductCommand request, CancellationToken ct)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("Product", request.Id);

        product.IsActive = false;
        await _db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}