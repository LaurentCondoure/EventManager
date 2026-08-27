namespace EventManager.InfrastructureTests.Fixtures;

/// <summary>
/// Single source of truth for container image tags used by the Testcontainers fixtures. Must
/// stay in sync with <c>infrastructure/docker/docker-compose.yml</c> — tests must run against
/// the same image versions as the application. There is no automated check for this; update
/// both places together when bumping a version.
/// </summary>
public static class ContainerImages
{
    public const string SqlServer = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string MongoDb = "mongo:7";
    public const string Redis = "redis:7-alpine";
    public const string Elasticsearch = "docker.elastic.co/elasticsearch/elasticsearch:9.0.2";
    public const string Varnish = "varnish:7";
}
