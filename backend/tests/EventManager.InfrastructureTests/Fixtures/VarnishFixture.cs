using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

using System.Text;

namespace EventManager.InfrastructureTests.Fixtures;

/// <summary>
/// Spins up two containers on an isolated Docker network:
///   - nginx  (alias "backend") — minimal HTTP mock returning JSON stubs with Cache-Control headers
///   - varnish                  — configured via the same VCL rules as production, pointing to "backend"
///
/// Tests hit Varnish through its mapped host port and check X-Cache headers.
/// </summary>
public class VarnishFixture : IAsyncLifetime
{
    private INetwork   _network = null!;
    private IContainer _backend = null!;
    private IContainer _varnish = null!;

    public HttpClient HttpClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _network = new NetworkBuilder()
            .WithName($"varnish-test-{Guid.NewGuid():N}")
            .Build();
        await _network.CreateAsync();

        _backend = new ContainerBuilder("nginx:alpine")
            .WithNetwork(_network)
            .WithNetworkAliases("backend")
            .WithResourceMapping(Encoding.UTF8.GetBytes(NginxConf), "/etc/nginx/nginx.conf")
            // nginx:alpine entrypoint prints this line just before launching the server.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("ready for start up"))
            .Build();

        await _backend.StartAsync();

        _varnish = new ContainerBuilder("varnish:7")
            .WithNetwork(_network)
            .WithPortBinding(80, assignRandomHostPort: true)
            .WithResourceMapping(Encoding.UTF8.GetBytes(VclContent), "/etc/varnish/default.vcl")
            // varnishd logs "Child launched OK" once the worker process is ready.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Child launched OK"))
            .Build();

        await _varnish.StartAsync();

        HttpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{_varnish.GetMappedPublicPort(80)}")
        };
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        await _varnish.DisposeAsync();
        await _backend.DisposeAsync();
        await _network.DeleteAsync();
    }

    // nginx returns minimal stubs — tests only check X-Cache headers, not response body.
    private const string NginxConf = """
        events {}
        http {
            server {
                listen 80;
                default_type application/json;

                location = /api/events {
                    add_header Cache-Control "public, max-age=300";
                    return 200 '[]';
                }

                location ~ "/full$" {
                    return 200 '{}';
                }

                location = /api/events/search {
                    return 200 '[]';
                }

                location ~ "^/api/events/[0-9a-fA-F-]{36}$" {
                    add_header Cache-Control "public, max-age=600";
                    return 200 '{}';
                }
            }
        }
        """;

    // Same routing rules as varnish/default.vcl — backend points to the nginx container alias.
    private const string VclContent = """
        vcl 4.1;

        backend default {
            .host = "backend";
            .port = "80";
        }

        sub vcl_recv {
            if (req.method != "GET" && req.method != "HEAD") {
                return (pass);
            }
            if (req.url ~ "^/api/events/search" || req.url ~ "/full($|\?)") {
                return (pass);
            }
            return (hash);
        }

        sub vcl_backend_response {
            if (bereq.url ~ "^/api/events(\?.*)?$") {
                set beresp.ttl = 5m;
                set beresp.grace = 30s;
            }
            else if (bereq.url ~ "^/api/events/[0-9a-fA-F-]{36}$") {
                set beresp.ttl = 10m;
                set beresp.grace = 30s;
            }
        }

        sub vcl_deliver {
            if (obj.hits > 0) {
                set resp.http.X-Cache = "HIT";
            } else {
                set resp.http.X-Cache = "MISS";
            }
            set resp.http.X-Cache-Hits = obj.hits;
        }
        """;
}
