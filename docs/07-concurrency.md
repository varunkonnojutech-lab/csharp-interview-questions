# Concurrency & Threading — Q40–Q45

## Q40. `lock` vs `SemaphoreSlim` vs `Monitor` vs `Mutex` — which and when?

- **`lock` (Monitor):** one thread at a time, same process, synchronous code only. The default for short critical sections.
- **`SemaphoreSlim`:** N threads at a time, and the only one of these that works with `await` (`WaitAsync`). Use it to throttle async work.
- **`Monitor`:** what `lock` compiles to — `Wait`/`Pulse` for signalling between threads.
- **`Mutex`:** works *across processes* (named mutex) — for single-instance apps, not for in-process sync.

```csharp
private readonly object _gate = new();
lock (_gate) { _balance += amount; }              // short, sync, in-process

private readonly SemaphoreSlim _throttle = new(3); // max 3 concurrent callers
await _throttle.WaitAsync();
try { await CallDownstreamAsync(); }
finally { _throttle.Release(); }
```

**Say in the interview:** "The rule is: `lock` for short synchronous sections, `SemaphoreSlim` when I need to await inside or limit concurrency, `Mutex` only across processes. And never `lock` on `this`, a string, or a Type — a private dedicated object, always."

**Listening for:** the await distinction, the lock-target rule.

> ▶ Runnable: `dotnet run -- concurrency` · proven by `SemaphoreSlim_NeverExceeds_ItsLimit`

## Q41. How do you write thread-safe code without locking everything?

Prefer data structures and patterns that are safe by construction: immutable objects (nothing to race on), `ConcurrentDictionary` / `ConcurrentQueue` for shared collections, `Interlocked` for counters, and confining mutable state to one thread (channels, actors). Locks are the last resort, kept short.

```csharp
// No lock: the collection itself is the synchronisation
var counts = new ConcurrentDictionary<string, int>();
counts.AddOrUpdate("hits", 1, (_, v) => v + 1);

// No lock: Interlocked is a single atomic CPU instruction
Interlocked.Increment(ref _totalRequests);

// No lock: immutable record — readers never see a half-written state
public record AppConfig(string ApiUrl, int TimeoutSeconds);
```

**Say in the interview:** "I design races out before I lock them out — immutable state, concurrent collections, and one owner per piece of mutable state. A lock around everything is correct but serialises the app; the interesting answer is knowing which pattern removes the lock entirely."

**Listening for:** immutability, concurrent collections, Interlocked, single-owner state.

> ▶ Runnable: `dotnet run -- concurrency` · proven by `ConcurrentDictionary_ParallelIncrements_AreExact`

## Q42. What's a race condition and a deadlock, and how do you diagnose each?

A **race condition** is when the outcome depends on thread timing — two threads read-modify-write the same value and one update is lost. A **deadlock** is when two threads each hold a lock the other needs and both wait forever. Races show up as wrong counts and flaky tests; deadlocks show up as hangs.

```csharp
// Race: two threads read 10, both write 11 — one increment is lost
_balance = _balance + 1; // not atomic: read, add, write

// Deadlock: lock order A→B in one thread, B→A in another
lock (a) { lock (b) { ... } }  // thread 1
lock (b) { lock (a) { ... } }  // thread 2 — both wait forever
```

**Say in the interview:** "For races I reproduce under load and look for the lost update — the fix is atomicity, usually Interlocked or a concurrent collection. For deadlocks I take a dump and look at the lock owners: `dotnet-dump` shows which thread holds which lock, and the fix is almost always a consistent lock order — or removing one of the locks."

**Listening for:** lost-update vs circular-wait, dump-based diagnosis, lock ordering.

## Q43. When do you use `Parallel.ForEach`, PLINQ, `Task`, or channels?

- **`Parallel.ForEach` / PLINQ:** CPU-bound work over in-memory data — image processing, crunching a list. Never for I/O.
- **`Task` / `Task.WhenAll`:** I/O-bound work — HTTP calls, DB queries, file reads.
- **Channels:** producer/consumer pipelines where work arrives over time and needs back-pressure.

```csharp
// CPU-bound: parallelise the computation
Parallel.ForEach(images, img => Resize(img));

// I/O-bound: start all calls, await together — do NOT use Parallel.ForEach here
var results = await Task.WhenAll(urls.Select(u => http.GetStringAsync(u)));

// Streaming: channel decouples a fast producer from a slow consumer
await foreach (var item in channel.Reader.ReadAllAsync()) await ProcessAsync(item);
```

**Say in the interview:** "The decision is CPU vs I/O. Parallel.ForEach on I/O just burns thread-pool threads waiting on the network — that's how you get thread-pool starvation. For I/O it's async tasks; for CPU it's parallel loops; for streams it's channels."

**Listening for:** CPU vs I/O as the deciding axis, the starvation consequence.

## Q44. How do channels (`System.Threading.Channels`) solve producer/consumer?

A channel is a thread-safe queue with async readers and writers, optional bounding, and back-pressure built in. The producer awaits `WriteAsync` when the channel is full; the consumer awaits `ReadAllAsync`. No locks, no `ManualResetEvent`, no polling loop.

```csharp
var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait // producer slows down instead of dropping work
});

_ = Task.Run(async () => { for (int i = 0; i < 1000; i++) await channel.Writer.WriteAsync(i); channel.Writer.Complete(); });
await foreach (var item in channel.Reader.ReadAllAsync()) await ProcessAsync(item);
```

**Say in the interview:** "Channels replaced the hand-rolled queue-plus-lock-plus-event pattern for me. The bounded mode is the key feature: when the consumer is slow, the producer waits instead of the queue growing until the process runs out of memory. It's the primitive behind most of the background processing I write now."

**Listening for:** back-pressure, no manual locking, bounded vs unbounded.

> ▶ Runnable: `dotnet run -- channels` · proven by `Channel_DeliversAllItems_InOrder`

## Q45. What is thread-pool starvation and how do you spot it?

The thread pool has a limited number of threads, and it grows slowly — roughly one new thread per second once the minimum is hit. If work blocks threads (`.Result`, `Thread.Sleep`, synchronous I/O, long `lock`s), requests queue up behind the blocked threads and latency explodes while CPU sits idle. That's starvation: threads exist but none are free.

```text
Symptoms: latency spikes under load, CPU low, "queue length" growing in dotnet-counters.
Causes:   .Result / .Wait() on async code, sync-over-async, blocking calls in async methods.
Fix:      async all the way down; Task.Run only for CPU work; raise MinThreads only as a band-aid.
```

**Say in the interview:** "I spot it in dotnet-counters — thread-pool queue length climbing while CPU is low is the signature. The fix is never 'add more threads' as the first move; it's removing the blocking — await instead of `.Result`, async I/O instead of sync I/O. Raising `ThreadPool.SetMinThreads` buys time, it doesn't fix the cause."

**Listening for:** the queue-length signature, low CPU + high latency, async as the fix.
**Follow-up:** "When is `Task.Run` the right call?" → CPU-bound work off the request thread, or isolating a blocking library you can't change.
