# EF Core & Data — Q28–Q33

## Q28. How do you avoid the N+1 query problem?

N+1 is one query for the list, then one query *per item* to load its related data — usually from lazy loading inside a loop. The fix is to load the related data in the same query: `Include` for a small graph, or a projection that fetches exactly the columns you need.

```csharp
// N+1: 1 query for orders + 100 queries for customers
foreach (var order in await db.Orders.ToListAsync())
    Console.WriteLine(order.Customer.Name); // lazy load per order!

// Fixed: one query, one round trip
var orders = await db.Orders.Include(o => o.Customer).ToListAsync();

// Better: projection — only the columns the screen needs
var summaries = await db.Orders
    .Select(o => new { o.Id, o.Total, Customer = o.Customer.Name })
    .ToListAsync();
```

**Say in the interview:** "I catch N+1s by logging the queries — `EnableSensitiveDataLogging` in dev, or a query counter in a test. The fix is Include or a projection, and the projection is usually better because it also stops over-fetching. Lazy loading is the thing I turn off by default in a new project."

**Listening for:** Include vs projection, logging queries, lazy loading off by default.

## Q29. When do you use EF Core vs Dapper vs raw SQL?

EF Core when the model is rich and change tracking, migrations, and LINQ queries earn their keep — most business apps. Dapper when the query is the product: reporting, heavy reads, stored procedures, or when you need to see the exact SQL. Raw SQL (via either) for the query the ORM can't express well.

```csharp
// EF Core: expressive, tracked, migrations included
var orders = await db.Orders.Where(o => o.Total > 100).ToListAsync();

// Dapper: exact SQL, minimal overhead — ideal for reports
var rows = await conn.QueryAsync<ReportRow>(
    "SELECT Region, SUM(Total) Revenue FROM Orders GROUP BY Region");
```

**Say in the interview:** "I default to EF Core for commands and Dapper for read-heavy reporting — the CQRS split, basically. The wrong answer is picking one for everything: EF Core generating a 400-line query for a report, or hand-writing CRUD SQL in Dapper for a simple entity."

**Listening for:** the CQRS-style split, knowing each tool's strength.
**Follow-up:** "What about performance?" → see [dapper-vs-efcore](https://github.com/varunkonnojutech-lab/dapper-vs-efcore) for BenchmarkDotNet numbers.

## Q30. What is change tracking, and when do you turn it off?

EF Core's change tracker snapshots every entity it loads, so `SaveChanges()` knows what to update. That snapshot costs memory and CPU — for read-only queries it's pure overhead, and for large result sets it can be the difference between fast and slow.

```csharp
// Tracking (default): needed when you plan to modify and SaveChanges
var order = await db.Orders.FindAsync(id);
order.Status = "Shipped";
await db.SaveChangesAsync();

// No-tracking: read-only — faster, less memory, no surprise updates
var orders = await db.Orders.AsNoTracking().Where(o => o.Total > 100).ToListAsync();
```

**Say in the interview:** "My default for queries is `AsNoTracking()` unless I intend to modify the entity — it's the single biggest free performance win in EF Core, and it also prevents the classic bug where a loaded entity gets modified by accident and saved by an unrelated `SaveChanges`."

**Listening for:** snapshot cost, AsNoTracking as default for reads, the accidental-save bug.

## Q31. How do you handle concurrency conflicts?

Optimistic concurrency: a `rowversion` (timestamp) column that the database bumps on every update. EF Core includes the original version in the `WHERE` clause; if another writer changed the row first, zero rows are affected and EF throws `DbUpdateConcurrencyException`. You then decide: retry, merge, or tell the user.

```csharp
public class Order
{
    public int Id { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = null!; // bumped by SQL Server
}

try { await db.SaveChangesAsync(); }
catch (DbUpdateConcurrencyException ex)
{
    // Someone else saved first. Reload, re-apply, or show "this record changed".
    await ex.Entries.Single().ReloadAsync();
}
```

**Say in the interview:** "I use optimistic concurrency everywhere two users can edit the same record — last-write-wins silently losing data is worse than a conflict error. Pessimistic locking (holding a DB lock while a user thinks) doesn't scale and doesn't survive a web request."

**Listening for:** rowversion, DbUpdateConcurrencyException, optimistic over pessimistic.

## Q32. Migrations in production — how do you deploy schema changes safely?

Never run `EnsureCreated` or auto-migrate from app startup in production. Generate a migration, review the SQL (`Script-Migration`), and apply it as a separate deployment step — before the code that needs it, in a backward-compatible way.

```text
Safe sequence:
  1. Deploy migration: ADD nullable column (old code still works)
  2. Deploy code: start writing/reading the new column
  3. Later migration: backfill + make it NOT NULL, drop the old column
```

**Say in the interview:** "The rule is expand-then-contract: additive changes first, code second, cleanup last — so a rollback at any step still works. I generate the SQL, read it, and run it in the pipeline; `dotnet ef database update` from a developer machine is how production databases get surprised."

**Listening for:** expand-then-contract, reviewed SQL, migration as a deployment step, rollback safety.

## Q33. How do you design an index, and how do you know a query is slow?

An index is a sorted copy of a column (plus included columns) that lets SQL Server seek instead of scan. Design from the query: equality columns first, then range columns, then `INCLUDE` the columns the query returns. You know a query is slow from the execution plan — look for scans, key lookups, and the costliest operator — and from `sys.dm_exec_query_stats`, not from guessing.

```sql
-- The query filters by CustomerId and returns Total + CreatedAt:
-- index on CustomerId, INCLUDE the returned columns, and the lookup disappears.
CREATE INDEX IX_Orders_CustomerId ON Orders (CustomerId) INCLUDE (Total, CreatedAt);
```

**Say in the interview:** "I start from the execution plan: scan means missing index, key lookup means the index doesn't cover the query. And I remember the trade-off — every index speeds reads and slows writes, so I index the queries that actually run, measured in production, not every column that looks indexable."

**Listening for:** seek vs scan, covering indexes, plan-driven decisions, the write cost.
**Follow-up:** "What about a query that's slow despite a good index?" → parameter sniffing, stale statistics, or a bad plan choice — check with `OPTION (RECOMPILE)` in a test.

> Deep dive: [sqlserver-efcore-pro](https://github.com/varunkonnojutech-lab/sqlserver-efcore-pro)
