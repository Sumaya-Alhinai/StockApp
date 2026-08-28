using Microsoft.EntityFrameworkCore;
using StockApp.Infrastructure.Persistence;

namespace StockApp.Tests;

public static class TestDbFactory
{
    public static string BuildConnectionString(string dbName)
        => $"Server=(localdb)\\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True";

    public static AppDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }

    public static AppDbContext CreateFresh(out string connectionString)
    {
        var dbName = "StockAppTest_" + Guid.NewGuid().ToString("N")[..8];
        connectionString = BuildConnectionString(dbName);

        var db = Create(connectionString);
        db.Database.EnsureCreated();
        return db;
    }
}