using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Common.Interfaces;

namespace StockApp.Application.Auth.Queries.Login;

public record LoginQuery(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(Guid Id, string FullName, string Email, string Token);

public class LoginQueryHandler : IRequestHandler<LoginQuery, LoginResult>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public LoginQueryHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<LoginResult> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var token = _jwt.CreateToken(user);

        return new LoginResult(user.Id, user.FullName, user.Email, token);
    }
}