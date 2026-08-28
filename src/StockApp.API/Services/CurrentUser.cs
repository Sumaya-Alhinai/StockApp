using System.Security.Claims;
using StockApp.Application.Common.Interfaces;

namespace StockApp.API.Services;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid Id
    {
        get
        {
            var value = _accessor.HttpContext?.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("User id claim missing.");
        }
    }
}