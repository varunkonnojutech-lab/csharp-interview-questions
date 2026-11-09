# ASP.NET Core & APIs — Q21–Q27

## Q21. How does the ASP.NET Core request pipeline work — and why does middleware order matter?

A request flows through middleware in the order you register it, then back out in reverse — like an onion. Each piece can short-circuit (return a response without calling `next`), or pass through. Order is the behaviour: authentication must run before authorization, exception handling must wrap everything, and static files should run before routing.

```csharp
var app = builder.Build();
app.UseExceptionHandler("/error");  // 1st: wraps everything below it
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();            // who are you? — before authorization
app.UseAuthorization();             // what may you do?
app.MapControllers();
```

**Say in the interview:** "The two ordering bugs I check for in review are auth-after-authorization — which silently lets anonymous requests through to authorization — and exception handling registered after the middleware that throws, so the exception escapes the handler. If I can draw the pipeline from memory, I can debug any request."

**Listening for:** onion model, short-circuiting, auth-before-authorization, exception handler first.

## Q22. What are the DI lifetimes — Singleton, Scoped, Transient — and what's the captive dependency trap?

- **Transient:** a new instance every time it's requested.
- **Scoped:** one instance per request (per scope).
- **Singleton:** one instance for the whole application lifetime.

The trap: a Singleton that depends on a Scoped service *captures* it — the scoped service (often a `DbContext`) lives forever, shared across requests, and is not thread-safe.

```csharp
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddSingleton<IReportCache, ReportCache>(); // must NOT depend on IOrderService

// If a Singleton genuinely needs scoped work, create a scope on demand:
using var scope = _serviceProvider.CreateScope();
var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
```

**Say in the interview:** "The default validation in ASP.NET Core actually throws when a Singleton captures a Scoped service, which is the framework telling you the design is wrong. My rule: DbContext is scoped, stateless services are singleton, and anything with per-request state is scoped — and I never inject a scoped service into a singleton."

**Listening for:** the three lifetimes, captive dependency, the built-in validation.

## Q23. How do you handle errors and return consistent API responses?

Unhandled exceptions should never leak stack traces or framework details. The pattern: a global exception handler (or middleware) that maps exceptions to problem details, plus a single envelope shape for errors so clients can code against it.

```csharp
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = ex switch
    {
        NotFoundException => (404, "Not found"),
        ValidationException => (400, "Validation failed"),
        _ => (500, "An unexpected error occurred")
    };
    await Results.Problem(title: title, statusCode: status).ExecuteAsync(context);
}));
```

**Say in the interview:** "Clients should get a stable contract: RFC 7807 Problem Details with a status, a title, and a trace id — never a stack trace, never a 500 with an empty body. And validation errors return 400 with the field errors, so the frontend can show them next to the right input."

**Listening for:** global handler, Problem Details, no stack-trace leaks, validation shape.

## Q24. How do you secure an API — JWT, refresh tokens, RBAC?

Short-lived access tokens (5–15 minutes) plus long-lived refresh tokens that can be rotated and revoked. The access token carries claims (user id, roles); the API validates the signature and expiry on every request, and authorization is enforced by policies, not by `if` statements in controllers.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Orders.Write", p => p.RequireRole("Admin", "OrderManager"));
```

**Say in the interview:** "Access tokens are short-lived and stateless; refresh tokens are stored server-side so they can be revoked — that's how logout and token theft are handled. Rotation means every refresh issues a new refresh token and invalidates the old one, so a stolen token can only be used once before the reuse is detected."

**Listening for:** short access + revocable refresh, rotation/reuse detection, policy-based authorization.
**Follow-up:** "Where do you store the refresh token?" → HttpOnly secure cookie for browsers, or a secure store for native clients — never localStorage.

> Deep dive: [jwt-auth-deepdive](https://github.com/varunkonnojutech-lab/jwt-auth-deepdive)

## Q25. Minimal APIs vs controllers — when do you choose which?

Minimal APIs are less ceremony: a route is a lambda, great for small services, prototypes, and microservices with a handful of endpoints. Controllers win when you need filters, model binding conventions, versioning attributes, and a team that knows where to find things — i.e. most real APIs.

```csharp
// Minimal API — one file, one endpoint, perfect for a small service
app.MapGet("/orders/{id}", async (int id, AppDbContext db) =>
    await db.Orders.FindAsync(id) is { } order ? Results.Ok(order) : Results.NotFound());

// Controller — structure scales: filters, versioning, conventions live here
[ApiController, Route("api/v1/[controller]"), Authorize(Policy = "Orders.Read")]
public class OrdersController : ControllerBase { ... }
```

**Say in the interview:** "I pick Minimal APIs for small services and internal tools, controllers for anything with more than a dozen endpoints or a team maintaining it. The mistake is treating it as a technical decision — it's a maintainability decision, and both compile to the same pipeline."

**Listening for:** ceremony vs structure, the team/maintainability framing.

## Q26. How do you version an API without breaking clients?

Version explicitly (URL, header, or query string), keep old versions running until clients migrate, and never change the meaning of an existing field. Deprecation is a communication act: sunset headers, docs, and a date.

```csharp
// URL versioning — the most visible and debuggable option
builder.Services.AddApiVersioning(o => o.DefaultApiVersion = new ApiVersion(1, 0))
    .AddApiExplorer(o => o.GroupNameFormat = "'v'VVV");

[ApiVersion("1.0"), ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/orders")]
public class OrdersController : ControllerBase
{
    [MapToApiVersion("2.0")]
    public IActionResult GetV2() => Ok(new { total = 0, currency = "USD" }); // additive field
}
```

**Say in the interview:** "The rule I follow is additive changes only within a version — new optional fields are fine, renaming or removing is a new version. And I version from day one, because bolting versioning onto a v1 that clients already depend on is the painful version of this interview question."

**Listening for:** additive-only within a version, explicit versioning, deprecation process.

> Deep dive: [api-versioning-docs](https://github.com/varunkonnojutech-lab/api-versioning-docs)

## Q27. How do you make an API fast — what do you measure first?

Measure before optimising: p95 latency, requests per second, and where the time goes (database, serialization, downstream calls). The fixes, in the order they usually pay off: kill N+1 queries, cache what doesn't change, return less data (paging, projection), make independent calls parallel, and only then look at the code.

```csharp
// Projection: fetch 3 columns, not the whole entity graph
var orders = await db.Orders
    .Where(o => o.CustomerId == id)
    .Select(o => new OrderSummary(o.Id, o.Total, o.CreatedAt))
    .Take(20)
    .ToListAsync();
```

**Say in the interview:** "My order is: measure with a profiler or Application Insights, fix the database first — it's the bottleneck in most APIs — then reduce payload size and parallelise independent calls. Micro-optimising C# before measuring is how you spend a week saving 2% while a missing index costs 90%."

**Listening for:** measure-first, database-first, projection/paging, the anti-premature-optimisation stance.
