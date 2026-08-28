using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Interfaces;
using StockApp.Domain.Enums;

namespace StockApp.Application.Products.Queries;

public record GetStockMovementsQuery(Guid ProductId) : IRequest<List<StockMovementItem>>;

public record StockMovementItem(
    Guid Id,
    MovementType MovementType,
    int Quantity,
    string? Note,
    DateTime CreatedAt);

public class GetStockMovementsQueryHandler
    : IRequestHandler<GetStockMovementsQuery, List<StockMovementItem>>
{
    private readonly IAppDbContext _db;

    public GetStockMovementsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<StockMovementItem>> Handle(
        GetStockMovementsQuery request, CancellationToken ct)
        => await _db.StockMovements
            .AsNoTracking()
            .Where(m => m.ProductId == request.ProductId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new StockMovementItem(
                m.Id, m.MovementType, m.Quantity, m.Note, m.CreatedAt))
            .ToListAsync(ct);
}