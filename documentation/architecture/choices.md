# Technology Choices — Rationale & Market Comparison

**Project:** EventManager — Formation 52h  
**Stack:** .NET 8 · SQL Server · MongoDB · Redis · Elasticsearch · Varnish · Vue.js 3 · Terraform · Azure DevOps

---

## 1. Relational Persistence — SQL Server

### Why SQL Server was retained

The project focuses its learning effort on cache management (Redis, Varnish) and Infrastructure as Code (Terraform on Azure), so I chose to keep a known technology here. Outside of this context, PostgreSQL would be the natural open-source default for a greenfield project.

**PostgreSQL** is the strongest alternative and the default choice for any cost-sensitive greenfield project. SQL Server and PostgreSQL are comparable on most workloads — both support recursive CTEs, window functions, and advanced indexes. PostgreSQL has the edge on JSON handling — its JSONB type is more expressive and performant than SQL Server's `FOR JSON`, which treats JSON as a plain string. Its open-source license removes a significant cost line, and .NET support via Npgsql is solid.

**MySQL** is widespread in LAMP-stack hosting environments. It lags behind both SQL Server and PostgreSQL — window functions and recursive CTEs were only introduced in version 8.0. The tooling and driver ecosystem are not optimised for a .NET context, which makes it rarely the right choice for a .NET backend.

**Oracle** is the enterprise incumbent, typically found in organisations with an existing Oracle investment. It is not a realistic starting point for a new project of this scale.

---

## 2. Document Persistence — MongoDB

### Why MongoDB was retained

MongoDB was chosen to store event comments because the data is semi-structured — free-form text of variable length — and the schema may evolve over time without requiring a migration. A document store is a better fit than a relational table for this pattern.

**CosmosDB** is the natural production choice on Azure — fully managed, zero-ops, and ~95% API compatible with MongoDB. The gap concerns advanced aggregation operators such as `$graphLookup` or `$facet`, and multi-document transaction options across multiple collections that this application does not use. MongoDB is kept for development to maintain full API parity.

**DynamoDB** is the AWS equivalent of CosmosDB. Since the project targets Azure, it was not studied further.

**SQL Server JSON columns** are a viable option when the relational engine is already in place — which is the case here. The limitation is that JSON is stored as plain `NVARCHAR`, making nested field queries more verbose and less performant than a dedicated document store.

---

## 3. Application Cache — Redis

### Why Redis was retained

Redis serves as the application-level cache using the cache-aside pattern: before querying SQL Server, the API checks whether the result is already stored in Redis. On a cache hit, the database is not touched. On a miss, the result is fetched and stored in Redis with a ten-minute TTL. When an event is created or modified, the relevant cache keys are invalidated immediately.

**Memcached** is the closest alternative for pure string caching. The limitation is that Redis supports rich data structures — lists, sets, hashes, streams — which makes it a better fit when cached objects are complex. Serialising a SQL Server object containing types like `geography` is more natural with Redis than with Memcached's string-only model.

**.NET 8 Output Caching** operates at a different level — it caches the full HTTP response, not the application object. It stores data in the .NET process memory, which means each API instance has its own independent cache. Redis is shared across all instances, which makes it the right choice in a multi-instance deployment.

**Azure Cache for Redis** is the natural production choice on Azure — fully managed, zero-ops, and the application code is unchanged. Redis is kept for local development via Docker. The cost of the basic tier (~€15/month) is not justified for a demonstration project that will be destroyed after deployment.

---

## 4. HTTP Cache — Varnish

### Why Varnish was retained

Varnish sits between the client and the API, caching complete HTTP responses for GET endpoints. It operates at the HTTP layer, which means the API itself requires no modification. This creates a second caching layer complementary to Redis: Redis caches at the object level inside the API, Varnish caches the full serialised HTTP response before the API is even invoked.

Varnish is used in this project specifically to demonstrate the HTTP caching layer in a visible and controllable way. In a real development environment it would be bypassed — a cache intercepting requests adds debugging overhead that slows down daily development.

**Nginx** can also act as an HTTP cache, but it is a secondary feature alongside its primary roles of reverse proxy, TLS termination, and static file serving. Varnish is purpose-built for HTTP caching, which makes it more expressive and more performant for this specific role. Nginx operates at the platform level and is typically shared across multiple applications — Varnish can be dedicated to a single application without constraining the rest of the ecosystem.

**Azure Front Door** is the natural production replacement on Azure — globally distributed, zero-ops, and it would replace Varnish entirely. As with Azure Cache for Redis, it is the right choice in production but not justified for a demonstration project.

**Cloudflare** adds DDoS protection and a WAF on top of the CDN capability. It is relevant when security and availability are primary constraints — for a high-traffic public-facing application or a sector with strong availability requirements. Neither applies here.

---

## 5. Full-Text Search — Elasticsearch

### Why Elasticsearch was retained

Elasticsearch powers the `/api/events/search` endpoint and provides ranked full-text search across the title, description, category, and artist name fields. Unlike a SQL `LIKE '%keyword%'` query which returns all matching rows with equal weight, Elasticsearch uses BM25 scoring — an algorithm that ranks results by term frequency and document length. A keyword match in a short title scores higher than the same keyword buried in a long description. The title boost (×2) amplifies this further — a match in the event name always outranks the same word in the description.

Elasticsearch is retained for its market relevance and transferable skill value, not for the technical needs of this project. For a dataset of a few hundred events with simple search requirements, Meilisearch would have been the simpler operational choice.

**Meilisearch** is purpose-built for small to medium datasets — fast, zero configuration, and easy to self-host. For this project it would have been technically sufficient. It becomes a less suitable choice when fine-grained scoring control, advanced aggregations, or production observability via Kibana are required.

**Azure Cognitive Search** is the natural production choice on Azure — fully managed, zero operational overhead. As with CosmosDB and Azure Front Door, it is the right choice in production but not justified for a demonstration project.

**SQL Server Full-Text Search** is a native SQL Server feature that uses dedicated full-text indexes and supports `CONTAINS` and `FREETEXT` instructions. It is more performant than `LIKE %` on large volumes, but it has no relevance scoring — all matching results are returned with equal weight, with no ranking by pertinence.

---

## 6. ORM / Data Access — Dapper

### Why Dapper was retained

Dapper is a micro-ORM that maps the results of explicit SQL queries to C# objects. It was chosen because the data schema is stable and defined upfront — EF Core migrations would bring no value here. Every query sent to SQL Server is visible in the repository code, which makes performance analysis and query plan inspection straightforward.

Dapper extends the standard ADO.NET `IDbConnection` interface, which means it works with any database provider — SQL Server, PostgreSQL, MySQL, SQLite — without additional configuration. Its parameterised query syntax naturally protects against SQL injection.

**Entity Framework Core 8** is the right choice when the schema is built from the code and evolves frequently — its code-first migrations handle schema changes automatically. On a stable schema with known queries, the generated SQL adds an abstraction layer that brings no value and makes query behaviour less predictable.

**Raw ADO.NET** provides the same level of control as Dapper but requires writing all the boilerplate manually — connection opening, command creation, result reading row by row, connection closing. The mapping from raw data to C# objects must also be handled manually, which Dapper does automatically via reflection. Testing is more complex: mocking requires setting up `IDbConnection`, `IDbCommand`, and `IDataReader` separately, whereas Dapper only requires mocking `IDbConnection`. In practice, ADO.NET also supports parameterised queries, but the verbosity increases the risk of forgetting to parameterise a query.

---

## 7. Backend Framework — ASP.NET Core (.NET 8)

### Why ASP.NET Core .NET 8 was retained

ASP.NET Core .NET 8 is the foundation of the .NET ecosystem for web applications and APIs. It is the target stack at GS1 France and France Billet, which makes it a direct business constraint. .NET 8 is the current LTS release, and compatibility with previous versions has been consistent since the unification of the runtime at .NET 5 — making it a safe foundation for enterprise projects migrating from older versions.

**NestJS** is a Node.js framework with an architecture similar to ASP.NET Core — controllers, services, dependency injection. Node.js runs server-side, not in the browser, despite sharing the same JavaScript runtime. It is a relevant choice for full-stack TypeScript teams, but outside the scope of this project which targets the .NET stack.

**FastAPI** is a Python framework. It requires Python knowledge that is outside the scope of this formation and this stack.

**Go** requires learning a different paradigm from scratch — no classes, no inheritance, explicit error handling. The learning cost is not justified in a formation already targeting a specific stack.

---

## 8. Validation — FluentValidation

### Why FluentValidation was retained

FluentValidation is widely adopted in the .NET ecosystem and provides composable, testable validator classes for incoming DTOs. Validators live in dedicated classes separate from the DTOs, which keeps the Domain layer free of validation concerns. Rules are written in C# — conditionals, cross-field rules, and composition are all natural. A validator can be instantiated and tested independently without spinning up the full API.

**DataAnnotations** are the native .NET validation mechanism — attributes placed directly on DTO properties. `System.ComponentModel.DataAnnotations` is part of the base framework and does not introduce any dependency on ASP.NET Core, making it usable in the Domain layer. Its limitation appears as soon as rules become conditional or cross-field — "ArtistName is required if the category is Concert" cannot be expressed cleanly with attributes. For simple required/max-length rules on small DTOs, DataAnnotations remain the simpler choice.

**MediatR Pipeline Validators** integrate validation into the MediatR dispatch pipeline — validators execute after the controller sends a command but before the handler processes it. This is only relevant in a project already using MediatR for CQRS. Introducing MediatR solely for its validation pipeline would add unnecessary infrastructure to a project that does not need the full CQRS pattern.

---

## 9. Logging — Serilog

### Why Serilog was retained

Serilog provides structured logging for the application. Unlike string-based logging, each property passed to Serilog is stored as a named, typed field rather than interpolated into a string. This opens a direct observability axis — Serilog's Elasticsearch sink indexes every log entry as a structured document, which allows Grafana or Kibana to build supervision dashboards on top of it: endpoint latency, error rates, request volumes — without any additional code in the application.

Serilog integrates via `Microsoft.Extensions.Logging` — the application code uses `ILogger<T>` throughout and never references Serilog directly. Serilog is wired in at the composition root only.

**Microsoft.Extensions.Logging (MEL)** is the native .NET logging abstraction and is what the application code uses directly. Used alone without a structured provider, it produces plain strings — the structured properties and the Elasticsearch integration that Serilog brings are not available.

**NLog** is a functional equivalent to Serilog with a similar sink system. It was not studied further as Serilog is the more widely adopted choice in the .NET ecosystem today.

---

## 10. Frontend Framework — Vue.js 3 + Pinia

### Why Vue.js 3 + Pinia was retained

Vue.js 3 is the most recent frontend framework used professionally. As with SQL Server, keeping a known technology on the frontend was a deliberate choice — the learning focus of this project is cache management and Infrastructure as Code, not the frontend stack.

Vue.js 3 Single File Components co-locate template, script, and style in a single `.vue` file, making the structure of a component immediately readable. The framework also integrates a native transition system for animating element appearance and disappearance. JavaScript was chosen intentionally over TypeScript for this project — TypeScript is a superset that compiles down to JavaScript via Vite, and understanding what runs in the browser matters. TypeScript will be introduced in another significant project where it is part of the target stack.

Pinia is the official state management library for Vue 3. It centralises application state — data fetched from the server is stored in the Pinia store and shared across all components without triggering additional HTTP requests. It replaces Vuex, which was the official store for Vue 2. Vuex imposed a strict separation between mutations and actions that made the code verbose. Pinia removes this concept — state is updated directly in actions. This simplification, combined with native TypeScript support, makes Pinia the natural choice for any new Vue 3 project. Vuex remains present on Vue 2 legacy projects, and migrating to Pinia is the recommended path when upgrading to Vue 3.

**React** is the dominant framework in the frontend job market, with a mature ecosystem and strong performance on complex applications. Its main entry barrier is the learning curve — JSX, hooks, and re-render management take time to master naturally.

**Angular** would have been a valid choice for a larger team with strict TypeScript discipline. For this MVP, the setup overhead and the mandatory RxJS learning curve are not justified.

---

## 11. Backend Testing — xUnit + Testcontainers

### Why xUnit + Testcontainers was retained

xUnit is the test framework recommended by Microsoft for .NET Core and above. Each test gets a fresh instance of the test class, which eliminates shared state between tests and makes test isolation explicit.

Testcontainers starts a real SQL Server Docker container during integration test execution. This ensures that repository tests run against the actual database engine — constraints, index behaviour, and T-SQL dialect are all tested as they would be in production. SQLite would have been technically sufficient for this MVP given the simplicity of the queries, but Testcontainers was chosen to demonstrate a more robust and transferable testing practice.

**SQLite in-memory** is useful for validating services and repositories without any infrastructure dependency. Its limitation is the dialect gap — queries specific to SQL Server such as `OFFSET/FETCH` or `GETUTCDATE()` would not work in SQLite. For this MVP the queries are simple enough that SQLite would likely work, but as a general principle, testing against the real engine is the safer choice.

**NUnit** is a functional equivalent to xUnit, widely present on legacy .NET projects. xUnit is preferred for new projects as it is the Microsoft recommendation for .NET Core and above.

---

## 12. Frontend Testing — Vitest

### Why Vitest was retained

Vitest is the native testing framework for Vue.js projects built on Vite. It shares the same configuration as the application — no separate bundler setup, no risk of divergence between the test environment and the build environment. It is the frontend equivalent of xUnit on the backend — a focused, framework-native testing tool.

**Jest** is the reference testing framework in the React ecosystem. On a Vite project, it requires a separate bundler configuration — Vitest eliminates this by reusing the Vite pipeline entirely.

**Playwright** is an end-to-end testing tool that simulates a real browser to test the application as a user would. It requires the full stack to be running — API, databases, deployed frontend. This infrastructure dependency makes it heavy to set up in a CI environment and out of scope for this project.

---

## 13. Infrastructure as Code — Terraform

### Why Terraform was retained

Terraform provisions the nine Azure resources required for production deployment through declarative HCL configuration files. It describes the desired infrastructure state, and creates, modifies or destroys resources via cloud APIs to match that state. The plan/apply workflow is the central value — `terraform plan` shows exactly what will change before any modification is made, enabling infrastructure changes to go through a review cycle in the same way as application code.

The project includes a local learning environment using the null provider — it simulates resources without calling any cloud API, which allows learning HCL syntax and the plan/apply workflow without an Azure account or any cost.

**Azure Bicep** is the native Azure alternative — simpler than ARM, Microsoft-supported, and it delegates state tracking to Azure Resource Manager directly, eliminating the need for a state file. It is limited to Azure only. Terraform was chosen here to demonstrate a multi-cloud portable skill, and because the plan/apply workflow is a key interview demonstration point.

**Pulumi** allows infrastructure to be described in a real programming language — TypeScript, Python, C#, or Go. This lowers the entry barrier for developers already familiar with these languages. The risk is that a full programming language allows writing complex conditional logic in infrastructure code, which can become difficult to read and maintain compared to Terraform's deliberately constrained HCL syntax.

**ARM Templates** are the native Azure format for infrastructure description, written in verbose JSON with no comments, no modules, and poor readability. Bicep was created precisely to address these limitations — it compiles down to ARM but no one writes ARM by hand today if Bicep is available.

---

## 14. CI/CD — Azure DevOps

### Why Azure DevOps was retained

Azure DevOps is the most recent CI/CD platform used professionally. As with SQL Server and Vue.js 3, keeping a known technology here was a deliberate choice — the learning focus of this project is cache management and Infrastructure as Code.

Azure DevOps pipelines are defined in YAML and version-controlled alongside the application code. The platform handles multiple repositories and multiple deliverables within the same project. Stages enable sequential actions subdivided into parallel jobs — build, test, and publish can be orchestrated independently while sharing artefacts between stages.

**GitHub Actions** follows the same YAML pipeline principles as Azure DevOps, integrated natively into GitHub repositories without a separate service to configure. It was not studied further as Azure DevOps covers the same needs and is the platform already in use.

**GitLab CI** was used professionally at Docaposte. It follows the same YAML pipeline principles, integrated natively into the GitLab platform — no separate CI server to install or maintain.

**Jenkins** was encountered before GitLab CI. Its setup and maintenance overhead — plugin management, server installation, agent configuration — made GitLab CI the preferred choice when it became available. Azure DevOps and GitLab CI both eliminate this server overhead by integrating CI/CD directly into the platform.

---

## 15. Containerisation — Docker / docker-compose

### Why Docker / docker-compose was retained

Docker and docker-compose provide the local development environment for all infrastructure services. In a distributed application context, each service — SQL Server, MongoDB, Redis, Elasticsearch, Varnish — runs in its own isolated container. Containers started via docker-compose communicate through Docker's internal network with a private DNS — the API container reaches SQL Server via `sqlserver:1433` without knowing its actual IP address. Each container runs autonomously and independently.

docker-compose orchestrates the entire stack in a single `docker-compose.yml` file. `docker-compose up -d` starts all services in one command, regardless of the host machine or operating system.

Docker is the de facto standard — universally known and supported across all projects and platforms.

**Podman** is a valid alternative in enterprise environments where Docker Desktop licensing is a concern — it is daemonless, rootless, and CLI-compatible with Docker. Docker was chosen here as the de facto standard.

**Azure Container Apps** is the natural production choice on Azure for containerised workloads — serverless scaling, zero infrastructure overhead. As with the other managed Azure services in this project, it is the right choice in production but not justified for a demonstration project.

**AKS** operates one level above docker-compose — it is a production orchestrator that guarantees containers run permanently and adapt to load. If a container crashes, Kubernetes restarts it automatically. If traffic increases, it starts new containers. Rolling updates deploy new versions without service interruption. docker-compose does none of this — it is a development tool, not a production orchestrator.
