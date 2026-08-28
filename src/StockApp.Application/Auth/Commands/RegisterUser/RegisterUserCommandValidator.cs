using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StockApp.Application.Common.Interfaces;

namespace StockApp.Application.Auth.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private readonly IAppDbContext _db;

    public RegisterUserCommandValidator(IAppDbContext db)
    {
        _db = db;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.")
            .MaximumLength(256)
            .MustAsync(BeUniqueEmail).WithMessage("This email is already registered.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return !await _db.Users.AnyAsync(u => u.Email == normalized, cancellationToken);
    }
}