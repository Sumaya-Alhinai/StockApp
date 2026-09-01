using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Interfaces;
using StockApp.Application.Common.Models;

namespace StockApp.Application.Products.Queries;

// ============================================================
// QUERY
// ============================================================

public record ListProductsQuery(
    string? Search,
    int PageNumber = 1,
    int PageSize = 10)
    : IRequest<PagedResult<ProductListItem>>;


// ============================================================
// DTO
// ============================================================

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


// ============================================================
// VALIDATOR
// ============================================================

public class ListProductsQueryValidator
    : AbstractValidator<ListProductsQuery>
{
    public ListProductsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage(
                "Page number must be greater than zero.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage(
                "Page size must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(100)
            .WithMessage(
                "Search cannot exceed 100 characters.");
    }
}


// ============================================================
// HANDLER
// ============================================================

public class ListProductsQueryHandler
    : IRequestHandler<
        ListProductsQuery,
        PagedResult<ProductListItem>>
{
    private readonly IAppDbContext _db;

    public ListProductsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ProductListItem>> Handle(
        ListProductsQuery request,
        CancellationToken ct)
    {
        var query = _db.Products
            .AsNoTracking();

        // --------------------------------------------------------
        // Search
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();

            query = query.Where(p =>
                EF.Functions.Like(
                    p.Name,
                    $"%{term}%")

                ||

                EF.Functions.Like(
                    p.SKU,
                    $"%{term}%")

                ||

                (p.Category != null &&
                 EF.Functions.Like(
                     p.Category,
                     $"%{term}%")));
        }

        // --------------------------------------------------------
        // Total count BEFORE Skip/Take
        // --------------------------------------------------------

        var totalCount = await query
            .CountAsync(ct);

        // --------------------------------------------------------
        // Pagination
        // --------------------------------------------------------

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(
                (request.PageNumber - 1)
                * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductListItem(
                p.Id,
                p.Name,
                p.SKU,
                p.Price,
                p.Category,
                p.IsActive,
                p.StockOnHand,
                Convert.ToBase64String(
                    p.RowVersion),
                p.CreatedAt))
            .ToListAsync(ct);

        // --------------------------------------------------------
        // Return paginated result
        // --------------------------------------------------------

        return new PagedResult<ProductListItem>
        {
            Items = items,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}