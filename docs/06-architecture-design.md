# Architecture & Design — Q34–Q39

## Q34. Clean Architecture vs vertical slices — which and when?

Clean Architecture organises by *layer* (Domain → Application → Infrastructure → API) so business rules don't depend on frameworks. Vertical slices organise by *feature* — one folder per use case, owning its endpoint, validation, and handler. Slices are simpler to navigate and change; layers are better when the domain is complex and shared across many features.

```text
Clean Architecture (by layer):
  src/Domain/Orders/Order.cs            src/Application/Orders/CreateOrder.cs
  src/Infrastructure/Persistence/       src/Api/Controllers/OrdersController.cs

Vertical slices (by feature):
  src/Features/Orders/CreateOrder.cs   // endpoint + validator + handler + tests, one folder
  src/Features/Orders/GetOrder.cs
  src/Features/Orders/ShipOrder.cs
```

**Say in the interview:** "For a CRUD-heavy API or a small team, I take vertical slices — a new feature is one folder, not four layers to touch. For a complex domain with rules shared across features, Clean Architecture earns its structure. The mistake is picking Clean Architecture for a simple app and spending the project wiring layers together."

**Listening for:** feature vs layer organisation, the complexity threshold, pragmatism over dogma.

> Deep dives: [clean-architecture-cqrs-template](https://github.com/varunkonnojutech-lab/clean-architecture-cqrs-template) · [vertical-slice-architecture](https://github.com/varunkonnojutech-lab/vertical-slice-architecture)

## Q35. How do you design a microservice boundary (DDD aggregates)?

A boundary follows the aggregate: a cluster of entities that must stay consistent together (Order + OrderLines), with one aggregate root that enforces the invariants. If two things must change in the same transaction, they belong in the same service; if they only need eventual consistency, they can be separate services communicating by events.

```csharp
// Order is the aggregate root — lines can't exist or change outside it
public class Order
{
    private readonly List<OrderLine> _lines = new();
    public IReadOnlyList<OrderLine> Lines => _lines;

    public void AddLine(Product product, int qty) // invariant enforced HERE
    {
        if (Status != OrderStatus.Draft) throw new InvalidOperationException("Only draft orders change");
        if (qty <= 0) throw new ArgumentOutOfRangeException(nameof(qty));
        _lines.Add(new OrderLine(product, qty));
    }
}
```

**Say in the interview:** "I draw boundaries around consistency needs, not around nouns in a diagram. The test is: can this service do its job without a synchronous call to another service? If it can't, the boundary is wrong. And the aggregate root is where the rules live — not in the controller, not in a service class."

**Listening for:** aggregate root, invariants, consistency as the boundary test, no cross-service synchronous calls.

> Deep dive: [ddd-tactical-patterns](https://github.com/varunkonnojutech-lab/ddd-tactical-patterns)

## Q36. How do you keep services consistent without distributed transactions?

You don't — two-phase commit across services is slow, fragile, and couples everything. Instead: the **transactional outbox** (save the change and the event in one local transaction, then a relay publishes it) and **sagas** (a sequence of local transactions with compensating actions when a later step fails).

```text
Order placed:
  1. Ordering service: INSERT order + INSERT outbox row (ONE transaction)
  2. Relay: publishes OrderPlaced event
  3. Payment service: charges → publishes PaymentCompleted (or PaymentFailed)
  4. On PaymentFailed: saga compensates — order is cancelled, stock released
```

**Say in the interview:** "The outbox is the trick that makes event publishing reliable: the event and the state change commit together, so you can never have an order without its event. And the saga is the honest answer to 'what if payment fails' — you don't roll back, you compensate forward."

**Listening for:** outbox atomicity, saga compensation, rejecting 2PC.

> Deep dives: [outbox-pattern](https://github.com/varunkonnojutech-lab/outbox-pattern) · [saga-choreography](https://github.com/varunkonnojutech-lab/saga-choreography)

## Q37. How do you handle failures between services?

Assume failure: networks fail, services deploy, downstream slows down. The toolkit is retries with backoff and jitter (for transient faults), circuit breakers (stop calling a service that's down), timeouts (never wait forever), and idempotency (so a retry doesn't charge twice).

```csharp
var pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
    .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
    {
        FailureRatio = 0.5, SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(30)
    })
    .AddTimeout(TimeSpan.FromSeconds(5))
    .Build();
```

**Say in the interview:** "Retries without jitter cause a thundering herd when the service comes back; a circuit breaker is what stops a slow dependency from taking your service down with it. And the one people forget is idempotency — every retryable operation needs an idempotency key, or the retry that succeeds twice is worse than the failure."

**Listening for:** backoff + jitter, circuit breaker, timeouts, idempotency keys.

> Deep dive: [resilience-with-polly](https://github.com/varunkonnojutech-lab/resilience-with-polly)

## Q38. Monolith vs microservices — how do you decide?

Start with a modular monolith: one deployable, well-separated modules inside it. Split into services when you have a concrete reason — independent scaling, independent deployment cadence, or a team boundary — not because microservices are the default. Every split adds network latency, distributed failure modes, and operational cost.

**Say in the interview:** "My test is: does this piece need to scale, deploy, or fail independently? If not, it's a module, not a service. The teams that regret microservices are the ones that split before they understood their domain boundaries — and then they pay the distributed-systems tax on every change."

**Listening for:** modular monolith first, concrete reasons to split, the cost of distribution.
**Follow-up:** "How do you know when to split?" → when one module's deploy cadence or scaling need starts hurting the rest, or when a team owns it end-to-end.

## Q39. How do you design an API that other teams can actually use?

Design from the consumer's job, not from your database. Stable resource names, consistent error shapes, pagination on every list, filtering and field selection, and docs with runnable examples. Then version it (Q26) and never break it silently.

```text
Good API checklist:
  - GET /orders?status=shipped&page=2&pageSize=20   (filtering + paging, always)
  - Errors: RFC 7807 Problem Details, same shape everywhere
  - Idempotency-Key header on POSTs that create things
  - OpenAPI spec published + example requests in the docs
  - Changelog + deprecation notice before any breaking change
```

**Say in the interview:** "The test of an API is whether another team can use it without asking me questions. That means examples in the docs, predictable errors, and pagination from day one — because the first time a client gets 100,000 rows in one response, the API is broken for them even if it works for me."

**Listening for:** consumer-first design, pagination, idempotency, docs with examples.
