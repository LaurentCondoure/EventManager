using EventManager.Infrastructure.Mappings;

using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace EventManager.InfrastructureTests.Fixtures;

public class MongoDbFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7")
        .Build();

    public IMongoClient Client { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        MongoDbMappings.Register();
        await _container.StartAsync();
        Client = new MongoClient(_container.GetConnectionString());
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
