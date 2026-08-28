using MediatR;
using Microsoft.AspNetCore.Mvc;
using StockApp.Application.Auth.Commands.RegisterUser;
using StockApp.Application.Auth.Queries.Login;

namespace StockApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterUserResult>> Register(
        RegisterUserCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login(
        LoginQuery query, CancellationToken ct)
        => Ok(await _mediator.Send(query, ct));
}