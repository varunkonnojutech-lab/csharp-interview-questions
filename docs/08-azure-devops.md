# Azure, DevOps & System Design — Q46–Q50

## Q46. How do you deploy a .NET app to Azure — App Service vs AKS vs Functions?

- **App Service:** a managed web host — push code, it runs. The default for APIs and web apps; scaling, slots, and TLS are built in.
- **AKS (Kubernetes):** when you need containers at scale, multiple services, and control over networking and scheduling.
- **Functions:** event-driven, pay-per-execution — webhooks, timers, queue triggers; not for long-running APIs.

```text
One API, one team            → App Service (or Container Apps)
Many services, one platform  → AKS + Helm, Ingress, HPA
"Run this when a file lands" → Function App, Blob trigger
```

**Say in the interview:** "I start with the simplest thing that meets the requirement — App Service for most APIs — and move to AKS when I need container orchestration across services, not before. The cost of Kubernetes is operational, and a team that doesn't need it pays that cost every week."

**Listening for:** matching the service to the shape of the workload, the operational-cost argument.

> Deep dives: [bicep-azure](https://github.com/varunkonnojutech-lab/bicep-azure) · [aks-helm-deploy](https://github.com/varunkonnojutech-lab/aks-helm-deploy)

## Q47. How do you keep secrets out of source code?

Secrets live in Azure Key Vault (or the platform's secret store), and the app reads them with a managed identity — no credentials in code, config, or environment variables that get pasted into a ticket. Locally, `dotnet user-secrets` or environment variables; never a committed `appsettings.json` with a real connection string.

```csharp
// No secret anywhere in this code — the identity IS the credential
builder.Configuration.AddAzureKeyVault(
    new Uri("https://myapp-vault.vault.azure.net/"),
    new DefaultAzureCredential()); // managed identity in Azure, dev login locally

var connectionString = builder.Configuration["Sql:ConnectionString"];
```

**Say in the interview:** "The rule is that a secret should exist in exactly one place — the vault — and reach the app through its identity, not through a value someone copied. I also scan for it: a pre-commit hook and a pipeline secret scan, because the one time a connection string lands in git history, rotating it is the only fix."

**Listening for:** managed identity, vault as single source, rotation after exposure.

## Q48. What does a good CI/CD pipeline for .NET look like?

Build once, test, then promote the same artifact through environments — never rebuild per environment. PRs run build + tests + lint; `main` builds the artifact and deploys to staging; production is a manual approval or a tag. Secrets come from the pipeline's secret store, and the deploy is a slot swap so rollback is instant.

```yaml
# .github/workflows/ci.yml in this repo — the shape every repo here uses
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: 8.0.x }
      - run: dotnet restore
      - run: dotnet build --no-restore -c Release
      - run: dotnet test --no-build -c Release
```

**Say in the interview:** "The principles are: the artifact that passed tests is the artifact that ships; environments differ by configuration, not by code; and deployment is boring — a slot swap with a health check, so a bad deploy rolls back in seconds instead of becoming an incident."

**Listening for:** build-once-promote-everywhere, config-per-environment, slot-swap rollback.

> Deep dive: [github-actions-patterns](https://github.com/varunkonnojutech-lab/github-actions-patterns)

## Q49. How do you observe a production .NET app?

Three signals, one story: **logs** (structured, with correlation ids, so one request can be traced), **metrics** (request rate, latency percentiles, error rate, GC and thread-pool health), and **traces** (a request's path across services). In Azure that's Application Insights + OpenTelemetry; the discipline is the same everywhere.

```csharp
// Structured logging: fields, not string soup — now searchable and aggregatable
logger.LogInformation("Order placed {OrderId} for {CustomerId} total {Total}",
    order.Id, order.CustomerId, order.Total);

// Correlation: every log line in a request carries the same trace id
builder.Services.AddOpenTelemetry().WithTracing(t => t.AddAspNetCoreInstrumentation());
```

**Say in the interview:** "I log with fields so I can query, not grep — `OrderId` as a field, not buried in a sentence. And I alert on symptoms users feel — error rate and p95 latency — not on CPU, which can be high and fine. The first thing I add to any new service is the correlation id, because debugging without it is guessing."

**Listening for:** structured logs, the three signals, symptom-based alerting, correlation ids.

## Q50. Walk me through how you'd design a URL shortener.

The closer — they want structure, not a perfect answer. Requirements first, then the design:

```text
Requirements: shorten a long URL, redirect fast, handle 100M URLs, ~100:1 read:write.
  1. Clarify: custom aliases? Expiry? Analytics? (Ask — don't assume.)
  2. API: POST /shorten { url } → { shortCode }; GET /{code} → 301 redirect.
  3. Code generation: base-62 counter (simple, sequential) or hash of the URL
     (collision handling needed); 6 chars ≈ 57B combinations.
  4. Storage: key-value store (code → URL), cache hot codes in Redis.
  5. Scale: stateless API behind a load balancer, DB sharded by code hash,
     redirect served from cache — the DB is almost never hit on reads.
  6. Reliability: idempotent POSTs (same URL → same code), rate limiting on
     shorten (abuse), analytics written async, not on the redirect path.
  7. Trade-offs: 301 vs 302 (permanent vs analytics), counter vs hash,
     what breaks first (the redirect cache, then the DB).
```

**Say in the interview:** "I start by clarifying — custom aliases and analytics change the design completely — then I draw the write path and the read path separately, because the read path is where the scale lives. And I name what breaks first: the cache, then the database. If I can say that, I've answered the question."

**Listening for:** clarifying first, read/write split, caching, naming the bottleneck, trade-offs stated out loud.

> Related: [rate-limiting-dotnet](https://github.com/varunkonnojutech-lab/rate-limiting-dotnet) (Q50's abuse control) · [redis-caching-dotnet](https://github.com/varunkonnojutech-lab/redis-caching-dotnet) (Q50's hot-code cache)
