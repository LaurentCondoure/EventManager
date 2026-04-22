# Redis — Concepts et Implémentation

## What is Redis
Redis (stand for **Re**mote **Di**ctionary **S**erver) is a KeyValuePair database which store data directly in **memory (RAM)** by default. It serves as the application-level cache.

Example : 
```
key          →   value
"event:123"  →   {"id":"123","title":"Concert Jazz",...}
"counter"    →   42
```

## principles
### Benefits
 Redis is used in our project as a caching layer between the database and the client to speed up data access and reduce the load on the main database. The When a client asks for data, the frontend part don't tequest the cache directly. The API forwards the request to Redis.
It then adresses two concern
+ Performances :
    frequently used data are cached so applications can retrieve it quickly without querying the main data source, in our case SQL Server
+ Security : 
    By reducing the connections, this benefits the resilience of the platform. In the event of unusually high traffic, the server is located behind a first security valve.

### Working with Redis
To work with redit in the .net solution, the nuget package StackExchange.Redis (documentation: https://stackexchange.github.io/StackExchange.Redis/)
1. Request Handling
After beeing requested, The API checks Redis (cache) to see if the requested data is already available.

2. Cache Hit
The data is found in Redis and successfully retrieve, the API retruns it immediatelyto the client.

3. Cache Miss
If the data is not present in Redis (nil returned) , the request is forwarded to the main database. The database processes the request and returns the required data to the application.

4. Cache Update
After fetching data from the database, it is stored in Redis for future use. This ensures that subsequent requests for the same data can be served faster.

### TTL (Time To Live)
After 10 minutes, Redis automatically deletes the key. Without TTL, the cache grows indefinitely and can become stale (obsolete data) forever.
```csharp
await _cache.StringSetAsync("event:123", json, TimeSpan.FromMinutes(10));
```

### JSON Serialization

Redis stores **bytes** — it doesn't know .NET types. You need to convert objects to string.

```csharp
// .NET Object → JSON → Redis
Event { Id = "123", Title = "Jazz Concert" }
→ {"id":"123","title":"Jazz Concert"}
→ stored under key "event:123"

// Redis → JSON → .NET Object
"event:123" → {"id":"123","title":"Jazz Concert"}
→ Event { Id = "123", Title = "Jazz Concert" }
```

#### Why JSON and not something else

| Format | Problem |
|---|---|
| Binary (BinaryFormatter) | Unreadable, deprecated in .NET 5+, versioning issues |
| XML | Verbose, much heavier |
| **JSON** | ✅ Readable, universal, lightweight, native with System.Text.Json |

JSON allows visual inspection of the cache with Redis CLI or RedisInsight.

### Errors Handling
Health check and Redis supervision have to be worked on

## To Go further
### Tools
+ Redis CLI
Redis provides an additional tool that allows interaction with an instance through an interface
https://redis.io/docs/latest/develop/tools/cli/
+ RedisInsight
RedisInsight is a more friendly interface for administration and supervision
https://redis.io/fr/redis-enterprise/redisinsight/





