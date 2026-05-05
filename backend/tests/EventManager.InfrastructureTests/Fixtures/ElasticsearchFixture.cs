using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Testcontainers.Elasticsearch;

namespace EventManager.InfrastructureTests.Fixtures;

public class ElasticsearchFixture : IAsyncLifetime
{
    private readonly ElasticsearchContainer _container = new ElasticsearchBuilder("docker.elastic.co/elasticsearch/elasticsearch:9.0.2")
        .Build();

    public ElasticsearchClient Client { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // ES 8.x uses HTTPS with a self-signed cert by default — AllowAll skips validation for the test container.
        var settings = new ElasticsearchClientSettings(new Uri(_container.GetConnectionString()))
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll);

        Client = new ElasticsearchClient(settings);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
