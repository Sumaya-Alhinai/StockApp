using StockApp.Domain.Entities;

namespace StockApp.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(User user);
}