using MediatR;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Exceptions;
using StockApp.Application.Common.Interfaces;
using StockApp.Domain.Entities;

namespace StockApp.Application.Auth.Commands.RegisterUser;

public record RegisterUserCommand(
    string FullName,
    string Email,
    string Password) : IRequest<RegisterUserResult>;

public record RegisterUserResult(Guid Id, string FullName, string Email);

public class RegisterUserCommandHandler
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public RegisterUserCommandHandler(IAppDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<RegisterUserResult> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("DUPLICATE_EMAIL", "This email is already registered.");
        }

        return new RegisterUserResult(user.Id, user.FullName, user.Email);
    }
}