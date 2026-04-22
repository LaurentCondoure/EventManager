# ADR-004: Cache-Aside Pattern for Application Caching

## Status
Accepted

## Context

The `EventManager` .NET solution requires a structural decision before development starts. The chosen architecture directly impacts testability, maintainability, and the ability to demonstrate clear separation of concerns in a technical interview context.

Key requirements:
- Add caching transparently without changing existing repository interfaces
- Maintain separation of concerns between data access and caching logic
- Enable easy testing and mocking of cache behavior
- Allow runtime configuration changes (enable/disable cache)
- Support multiple cache implementations if needed

### Alternatives
#### Alternative 1: Write-Through
Every data are stored in both the database and the cache. Write is considered successfully complete when data are successfully saved in both data store.
In one hand, it allow strong data consistency between the cache and the database and make debbuging easier. On the other hand, every data are cached even they had not be read by users and errors managment 

#### Alternative 2: **cache aside** 
The database is the trusted data source, the cache is built around this principle. When data are Inserted or updated, the database is the modified and the cache invalidated. Next time a user read those data, the cache will miss and the data will be retrieve from database. 
Neitherless, cache invalidation can be difficult to manage if too many entry are impacted. This can cause a partial cache invalidation making some data stale, or an overly broad invalidation that would harm cache efficiency. Similarly, if the data happens to be too volatile, the cache becomes a constraint without benefit.

#### Alternative 3: **Write-behind**  

**Write-behind** is another pattern which has not been studied much further, notably because of its major weaknesses (Risk of data loss combined with Complex failure handlin, debugging and testing complexity)


## Decision
The project is a technical demonstration targeting senior developer interviews. We assume events won't change often during their lifetime.

The cache pattern follow the recommandations from Microsoft Learning :
https://learn.microsoft.com/en-us/azure/architecture/patterns/cache-aside

### Implementation Example

```csharp
//Insert / Update Principle
public async Task<Guid> CreateAsync(Event @event)
{
    //Database Insert
    //Cache invalidation
    //return id of created event
}

//Select Principle
public async Task<Event?> GetByIdAsync(Guid id)
{
    //Get cached data

    if (cached.HasValue)
        //return cached

    //Select from database
    var @event = await _inner.GetByIdAsync(id);

    //set cache
    return @event;
}
```

## Consequences
- Cached serch won't be invalidate. Users could see obsolete data on search page, but those would be refresh when an event is read
- Cache is invalidate after a change in database due to the hight criticity of the price. a price evolution is to define it by seat type and to change its domain. Thus the cache-aside pattern can be used with the write-through pattern  
- The TTL must be finely set to avoid pitfalls
