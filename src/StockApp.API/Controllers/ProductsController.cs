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

    [HttpGet]
    public async Task<ActionResult<List<ProductListItem>>> List(
        [FromQuery] string? search, CancellationToken ct)
        => Ok(await _mediator.Send(new ListProductsQuery(search), ct));

    [HttpGet("{id:guid}/movements")]
    public async Task<ActionResult<List<StockMovementItem>>> Movements(
        Guid id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetStockMovementsQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        CreateProductCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, UpdateProductCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest();
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteProductCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeactivateProductCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/adjust-stock")]
    public async Task<ActionResult<int>> AdjustStock(
        Guid id, AdjustStockCommand command, CancellationToken ct)
    {
        if (id != command.ProductId) return BadRequest();
        return Ok(await _mediator.Send(command, ct));
    }
}