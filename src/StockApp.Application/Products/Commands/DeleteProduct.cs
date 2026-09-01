using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Common.Interfaces;

namespace StockApp.Application.Products.Commands;

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
            product.IsActive = false;
            await _db.SaveChangesAsync(ct);

            throw new ConflictException(
                "DELETE_BLOCKED",
                "This product has stock movement history and cannot be deleted. It has been deactivated instead.");
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}

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