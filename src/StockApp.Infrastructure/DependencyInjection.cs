using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockApp.Application.Common.Interfaces;
using StockApp.Infrastructure.Persistence;
using StockApp.Infrastructure.Security;

namespace StockApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtSettings jwtSettings)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton(jwtSettings);
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}