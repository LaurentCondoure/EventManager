using StackExchange.Redis;
using Testcontainers.Redis;

namespace EventManager.InfrastructureTests.Fixtures;

public class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine")
        .Build();

    public IConnectionMultiplexer Connection { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Connection = await ConnectionMultiplexer.ConnectAsync(_container.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
        await _container.DisposeAsync();
    }
}
