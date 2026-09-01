using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockApp.Application.Products.Commands;
using StockApp.Application.Products.Queries;

namespace StockApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ============================================================
    // LIST PRODUCTS WITH PAGINATION
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new ListProductsQuery(
                search,
                pageNumber,
                pageSize),
            ct);

        return Ok(result);
    }

    // ============================================================
    // STOCK MOVEMENTS
    // ============================================================

    [HttpGet("{id:guid}/movements")]
    public async Task<ActionResult<List<StockMovementItem>>> Movements(
        Guid id,
        CancellationToken ct)
        => Ok(await _mediator.Send(
            new GetStockMovementsQuery(id),
            ct));

    // ============================================================
    // CREATE
    // ============================================================

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        CreateProductCommand command,
        CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    // ============================================================
    // UPDATE
    // ============================================================

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest();

        await _mediator.Send(command, ct);

        return NoContent();
    }

    // ============================================================
    // DELETE
    // ============================================================

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken ct)
    {
        await _mediator.Send(
            new DeleteProductCommand(id),
            ct);

        return NoContent();
    }

    // ============================================================
    // DEACTIVATE
    // ============================================================

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken ct)
    {
        await _mediator.Send(
            new DeactivateProductCommand(id),
            ct);

        return NoContent();
    }

    // ============================================================
    // ADJUST STOCK
    // ============================================================

    [HttpPost("{id:guid}/adjust-stock")]
    public async Task<ActionResult<int>> AdjustStock(
        Guid id,
        AdjustStockCommand command,
        CancellationToken ct)
    {
        if (id != command.ProductId)
            return BadRequest();

        return Ok(await _mediator.Send(
            command,
            ct));
    }
}