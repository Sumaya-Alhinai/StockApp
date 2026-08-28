using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Interfaces;

namespace StockApp.Application.Products.Queries;

public record ListProductsQuery(string? Search) : IRequest<List<ProductListItem>>;

public record ProductListItem(
    Guid Id,
    string Name,
    string SKU,
    decimal Price,
    string? Category,
    bool IsActive,
    int StockOnHand,
    string RowVersion,
    DateTime CreatedAt);

public class ListProductsQueryHandler
    : IRequestHandler<ListProductsQuery, List<ProductListItem>>
{
    private readonly IAppDbContext _db;

    public ListProductsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ProductListItem>> Handle(
        ListProductsQuery request, CancellationToken ct)
    {
        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();

            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{term}%") ||
                EF.Functions.Like(p.SKU, $"%{term}%") ||
                (p.Category != null && EF.Functions.Like(p.Category, $"%{term}%")));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductListItem(
                p.Id,
                p.Name,
                p.SKU,
                p.Price,
                p.Category,
                p.IsActive,
                p.StockOnHand,
                Convert.ToBase64String(p.RowVersion),
                p.CreatedAt))
            .ToListAsync(ct);
    }
}