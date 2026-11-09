# Runtime, GC & Memory — Q15–Q20

## Q15. How does the .NET garbage collector work — generations, LOH, compaction?

The GC assumes most objects die young, so it organises the heap into generations. Gen 0 is collected often and cheaply; survivors get promoted to Gen 1, then Gen 2, which is collected rarely and is the expensive one. Large objects (85,000+ bytes) go straight to the Large Object Heap, which historically wasn't compacted, so it fragments.

```text
new object()  → Gen 0
survives a Gen 0 collection → Gen 1
survives a Gen 1 collection → Gen 2
85,000+ bytes → Large Object Heap (LOH)
```

**Say in the interview:** "The practical rule is: short-lived objects are nearly free — the GC is designed for them. The expensive things are long-lived Gen 2 objects and LOH fragmentation from repeatedly allocating large arrays. That's why I reuse buffers with `ArrayPool<T>` and avoid allocating big strings in a loop."

**Listening for:** generational hypothesis, Gen 2 cost, LOH threshold and fragmentation.
**Follow-up:** "Server GC vs Workstation GC?" → Server GC gives each core its own heap — higher throughput, more memory; Workstation GC is the default for desktop, lower latency.

## Q16. What is a memory leak in .NET, and how do you find one?

.NET doesn't leak memory the way C++ does — it leaks *references*. An object that is still reachable can't be collected, so a static cache that never evicts, an event subscription that is never removed, or a `Timer` that is never disposed will grow forever.

```csharp
// The classic leak: subscribing and never unsubscribing pins the subscriber alive
publisher.DataChanged += subscriber.OnDataChanged; // subscriber can never be collected now
publisher.DataChanged -= subscriber.OnDataChanged; // the fix — or use a weak event
```

**Say in the interview:** "I find them with a dump, not with guessing: take two dumps an hour apart, `dotnet-dump` + `!dumpheap -stat`, and the type whose count grew is the leak. Then I walk the roots — `!gcroot` — to find what's holding it. The usual suspects are static collections, event handlers, and caches without eviction."

**Listening for:** reachable = not collectable, dump-driven diagnosis, the event-handler trap.
**Follow-up:** "How do you prevent it?" → bounded caches with eviction, weak references for pub/sub, dispose timers.

## Q17. What is `IDisposable`, and what's the difference between `using`, `using var`, and finalizers?

`IDisposable` is the contract for deterministic cleanup — releasing unmanaged resources (file handles, sockets, DB connections) *now*, instead of waiting for the GC. `using` calls `Dispose()` at the end of its block; `using var` does the same at the end of the enclosing scope, in reverse declaration order. A finalizer (~ClassName) is the last-resort backup the GC runs — non-deterministic, expensive, and no substitute for disposing.

```csharp
using (var conn = new SqlConnection(cs)) { ... } // disposed at the closing brace

using var conn = new SqlConnection(cs);          // disposed at end of scope, reverse order
using var cmd = new SqlCommand(sql, conn);
```

**Say in the interview:** "If it implements `IDisposable`, I dispose it — usually with `using var`. The finalizer is a safety net, not a plan: it runs on the GC thread, at an unknown time, and an object with a finalizer survives one extra collection. And `IAsyncDisposable` exists for the same reason when cleanup itself is async."

**Listening for:** deterministic vs non-deterministic cleanup, reverse-order disposal, finalizer cost.

> ▶ Runnable: `dotnet run -- disposal` · proven by `Disposal_UsingDisposes_InReverseDeclarationOrder`

## Q18. What is string interning, and how does `string` equality actually work?

The runtime keeps an intern pool of literal strings: every occurrence of `"hello"` in your code is the same object. `==` on strings compares *content* (it's overloaded), while `ReferenceEquals` compares identity — which is why interned literals pass `ReferenceEquals` and dynamically built strings don't. Never write logic that depends on interning.

```csharp
ReferenceEquals("hello", "hello");              // true — same interned literal
ReferenceEquals("hel" + "lo", "hello");          // true — compile-time constant, interned
ReferenceEquals(new string("hello".ToCharArray()), "hello"); // false — new object, same content
"hello" == new string("hello".ToCharArray());    // true — == compares content
```

**Say in the interview:** "Interning is a memory optimisation for literals, not a feature to rely on. The thing that matters in interviews is that `string ==` is value equality while `==` on most other reference types is identity — and that `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` is the correct way to compare user input, never `ToLower() ==`."

**Listening for:** intern pool as optimisation, `==` overload on string, Ordinal comparison.

## Q19. How do `Span<T>` and `Memory<T>` avoid allocations?

`Span<T>` is a `ref struct` — a (pointer, length) view over existing memory: an array, a stackalloc buffer, or unmanaged memory. Slicing it creates another view, not a copy. `Memory<T>` is the heap-safe counterpart you can store in a field or pass across an await. Together they let you parse, slice, and copy without allocating.

```csharp
ReadOnlySpan<char> text = "Hello, World!".AsSpan();
var hello = text[..5];                 // a view — zero allocation
int.TryParse("42".AsSpan(), out int n); // parses without allocating a string
Span<int> buffer = stackalloc int[8];  // stack buffer, no heap at all
```

**Say in the interview:** "I reach for Span when profiling shows string or array allocations in a hot path — parsing, protocol handling, high-throughput APIs. The constraint to remember is that Span can't cross an `await` or live in a field — that's what `Memory<T>` is for."

**Listening for:** view-not-copy, the ref-struct constraint, Memory<T> as the storable form.

> ▶ Runnable: `dotnet run -- span` · proven by `Span_SlicingAndParsing_WorkWithoutAllocatingStrings`

## Q20. What causes Gen 2 GC pressure, and how do you reduce it?

Gen 2 collections happen when Gen 0 and Gen 1 can't free enough, or when the LOH fills up. The causes are almost always the same: large long-lived allocations, caches that grow without bounds, string building in loops, and allocating big arrays per request instead of reusing buffers.

```csharp
// Per-request 100 KB buffer → LOH, Gen 2 pressure under load
var buffer = new byte[100_000];

// Reuse instead — ArrayPool returns buffers to a shared pool
var buffer = ArrayPool<byte>.Shared.Rent(100_000);
try { /* use buffer */ }
finally { ArrayPool<byte>.Shared.Return(buffer); }
```

**Say in the interview:** "I measure first — `% Time in GC` and Gen 2 collection counts in dotnet-counters — then I fix the biggest allocator, which is usually string concatenation in a loop, per-request large buffers, or a cache without eviction. `ArrayPool<T>`, `StringBuilder`, and pooling are the fixes; the GC itself is rarely the problem."

**Listening for:** measure-first, the usual suspects, ArrayPool as the fix.
**Follow-up:** "When is it actually the GC's fault?" → almost never; if it is, look at Server vs Workstation GC and the latency modes before touching anything else.
