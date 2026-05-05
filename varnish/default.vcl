vcl 4.1;

# API running on the host machine — host.docker.internal resolves to the Docker host.
# On Linux: requires extra_hosts in docker-compose (host.docker.internal:host-gateway).
# On Docker Desktop (Windows/Mac): available automatically.
backend default {
    .host = "host.docker.internal";
    .port = "5256";
    .connect_timeout = 5s;
    .first_byte_timeout = 30s;
    .between_bytes_timeout = 10s;
}

sub vcl_recv {
    # Mutations (POST, PUT, DELETE) must always reach the API — never cache.
    if (req.method != "GET" && req.method != "HEAD") {
        return (pass);
    }

    # Search results vary by query string — bypass cache.
    # /full aggregates live comment data — bypass cache.
    if (req.url ~ "^/api/events/search" || req.url ~ "/full($|\?)") {
        return (pass);
    }

    return (hash);
}

sub vcl_backend_response {
    # List endpoint: GET /api/events and GET /api/events?page=N&pageSize=M
    if (bereq.url ~ "^/api/events(\?.*)?$") {
        set beresp.ttl = 5m;
        set beresp.grace = 30s;
    }
    # Single event: GET /api/events/{guid}
    # Matches a 36-character GUID and nothing after (excludes /full, /search, etc.)
    else if (bereq.url ~ "^/api/events/[0-9a-fA-F-]{36}$") {
        set beresp.ttl = 10m;
        set beresp.grace = 30s;
    }
}

sub vcl_deliver {
    # X-Cache header lets clients and developers observe cache behaviour.
    if (obj.hits > 0) {
        set resp.http.X-Cache = "HIT";
    } else {
        set resp.http.X-Cache = "MISS";
    }
    set resp.http.X-Cache-Hits = obj.hits;
}
