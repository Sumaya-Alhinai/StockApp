using StockApp.Application.Common.Interfaces;

namespace StockApp.Tests;

public class FakeCurrentUser : ICurrentUser
{
    public Guid Id { get; }

    public FakeCurrentUser(Guid id) => Id = id;
}