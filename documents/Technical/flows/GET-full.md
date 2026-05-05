```mermaid
sequenceDiagram
    participant Client
    participant Router as ASP.NET Router
    participant EH as ExceptionHandler
    participant API as EventsController
    participant Service as EventService
    participant Cache as CachedEventRepository
    participant DB as SQL Server
    participant Repo as MongoDbCommentRepository
    participant Mongo as MongoDB

    Client->>Router: GET /api/events/{id}/full
    alt {id} is not a valid GUID
        Router-->>Client: 404 Not Found (route not matched — format not disclosed)
    else {id} is a valid GUID
        Router->>API: GET /api/events/{id}/full
        note over API,Mongo: Not cached by Varnish — comments are live data
        API->>Service: GetWithCommentsAsync(id)
        Service->>Cache: GetByIdAsync(id)
        Cache->>DB: SELECT * FROM Events WHERE Id = {id}
        alt Event not found
            DB-->>Cache: null
            Cache-->>Service: null
            Service-->>EH: NotFoundException
            EH-->>Client: 404 Not Found
        else Event found
            DB-->>Cache: Event
            Cache-->>Service: Event
            Service->>Repo: GetByEventIdAsync(id)
            Repo->>Mongo: Find({ eventId }) sort createdAt desc
            alt DB error
                Mongo-->>EH: Exception
                EH-->>Client: 500 Internal Server Error
            else DB ok
                Mongo-->>Repo: IEnumerable<EventComment>
                Repo-->>Service: IEnumerable<EventComment>
                Service-->>API: EventWithCommentsDto
                API-->>Client: 200 OK
            end
        end
    end
```
