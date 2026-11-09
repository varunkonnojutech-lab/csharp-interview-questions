# Async / Await & Tasks — Q8–Q14

## Q8. What happens when you `await` a Task — what does the compiler generate?

`await` doesn't block the thread. The compiler rewrites your method into a state machine: at the `await`, if the task isn't finished, the method returns and the rest of your code is registered as a continuation. When the task completes, the continuation runs — on the captured context, unless you asked otherwise.

```csharp
async Task<string> GetOrderAsync(int id)
{
    var order = await _db.Orders.FindAsync(id); // thread is freed here
    return order.Reference;                     // continuation runs when the query finishes
}
```

**Say in the interview:** "Async is about freeing the thread while I/O is in flight, not about parallelism. The state machine is why `async` methods can't have `ref`/`out` parameters or be used in a `lock` — the method can return mid-way, so the stack frame has to be heap-allocated."

**Listening for:** state machine, freed thread, continuation — not "it runs on another thread".
**Follow-up:** "So what actually runs in parallel?" → `Task.Run` / `Task.WhenAll` — starting work, not awaiting it.

## Q9. Why is `async void` dangerous, and where is it allowed?

`async void` can't be awaited, so exceptions thrown inside it have nowhere to go — they crash the process (or get swallowed, depending on the context). You also can't tell when it finished, so tests and callers can't observe it. The only legitimate use is event handlers, where the signature is forced on you.

```csharp
button.Click += async (s, e) => { await SaveAsync(); }; // OK — event handler, no choice
public async void Save() { ... }                        // NO — caller can't await or catch
public async Task SaveAsync() { ... }                   // YES
```

**Say in the interview:** "The rule I follow is `async Task` everywhere except event handlers. If I see `async void` in a review, the fix is almost always to return `Task` and let the caller await it — and to check the exception handling, because an `async void` crash looks like a random process exit in the logs."

**Listening for:** unobservable exceptions, the event-handler exception.

## Q10. What's the difference between `Task`, `ValueTask`, and `Task<T>`?

`Task<T>` is a heap-allocated object representing async work that produces a value. `Task` is the same without a value. `ValueTask<T>` is a struct that can wrap either a `T` already available or a `Task<T>` — it avoids the allocation when the result is ready synchronously, which matters in hot paths (e.g. a cache hit).

```csharp
// Cache hit returns synchronously — ValueTask avoids allocating a Task for it
public ValueTask<User?> GetUserAsync(int id)
{
    if (_cache.TryGetValue(id, out var user)) return new ValueTask<User?>(user);
    return new ValueTask<User?>(LoadFromDbAsync(id));
}
```

**Say in the interview:** "I default to `Task<T>`. I reach for `ValueTask<T>` when profiling shows Task allocations in a hot path — typically a cache or a fast-path check — and I never `await` a `ValueTask` twice, because it may only be consumed once."

**Listening for:** allocation avoidance, the consume-once rule, defaulting to Task.

## Q11. Why does `.Result` / `.Wait()` deadlock, and what changed in ASP.NET Core?

In classic ASP.NET and UI apps there is a synchronization context that runs continuations on one thread. `.Result` blocks that thread waiting for the task; the task's continuation needs the same thread to run. Neither can proceed — deadlock. ASP.NET Core has **no synchronization context**, so the same code usually *doesn't* deadlock there — but it still blocks a thread-pool thread, which is how you get thread-pool starvation under load instead.

```csharp
// Classic ASP.NET / WinForms / WPF: deadlocks. ASP.NET Core: works, but wastes a thread.
var order = GetOrderAsync(id).Result;

// The fix is always the same — await all the way up:
var order = await GetOrderAsync(id);
```

**Say in the interview:** "The deadlock needs two ingredients: a single-threaded context and a blocking wait. ASP.NET Core removed the context, so the deadlock went away — but `.Result` still blocks a thread, so under load it becomes starvation. The rule stays the same: async all the way down, never block on async code."

**Listening for:** the two ingredients, the ASP.NET Core difference, starvation as the modern failure.
**Follow-up:** "What if you genuinely can't make the caller async?" → offload to `Task.Run` and block on *that* (accepting the cost), or redesign the boundary.

## Q12. `Task.WhenAll` vs `Task.WhenAny` vs `await` in a loop — which and when?

Awaiting in a loop serialises independent calls: each one waits for the previous. `Task.WhenAll` starts them all and awaits the group — total time is the slowest call, not the sum. `Task.WhenAny` returns when the *first* finishes — for timeouts, races, and "first good answer wins".

```csharp
// Sequential: ~300 ms for three 100 ms calls
var a = await GetAAsync(); var b = await GetBAsync(); var c = await GetCAsync();

// Parallel: ~100 ms — start all three, then await together
var (a, b, c) = await (GetAAsync(), GetBAsync(), GetCAsync()); // or Task.WhenAll

// First answer wins, with a timeout
var winner = await Task.WhenAny(GetAAsync(), Task.Delay(500).ContinueWith(_ => "timeout"));
```

**Say in the interview:** "The decision is about dependency: if B needs A's result, await in order; if they're independent, start them all. And `WhenAll` fails fast on the first exception — if I need every result including the failures, I await each task individually after `WhenAll` throws, or wrap them."

**Listening for:** dependency vs independence, the fail-fast behaviour of WhenAll.

> ▶ Runnable: `dotnet run -- async` · proven by `WhenAll_IsFaster_ThanSequentialAwaits`

## Q13. How do you cancel async work properly?

Pass a `CancellationToken` through the whole call chain and hand it to every await that accepts one — `Task.Delay`, `HttpClient`, EF Core's `ToListAsync(ct)`. Check it in loops with `ThrowIfCancellationRequested()`. The caller owns the timeout via `CancellationTokenSource`.

```csharp
public async Task<int> CountToAsync(int target, CancellationToken token)
{
    for (int i = 0; i < target; i++)
    {
        token.ThrowIfCancellationRequested();
        await Task.Delay(10, token); // Delay itself honours the token
    }
    return target;
}

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await CountToAsync(1000, cts.Token); // throws OperationCanceledException at ~5 s
```

**Say in the interview:** "Cancellation is cooperative — the token is just a signal, and the work has to check it. The two mistakes I see are swallowing `OperationCanceledException` as if it were an error, and creating a token nobody passes anywhere. In ASP.NET Core, `HttpContext.RequestAborted` is a free token for client disconnects."

**Listening for:** cooperative cancellation, passing the token into awaits, `OperationCanceledException` as normal flow.

> ▶ Runnable: `dotnet run -- cancel` · proven by `CancellationToken_StopsWork_WhenCancelled`

## Q14. What is `ConfigureAwait(false)` and when do you still need it?

`await` captures the current synchronization context and resumes on it — in a UI app that's the UI thread, in classic ASP.NET the request thread. `ConfigureAwait(false)` says "resume on any thread-pool thread". In ASP.NET Core there is no context, so it's a no-op there; in libraries and UI code it still matters, both for deadlocks and for not hogging the UI thread.

```csharp
// Library code: no need to return to the caller's context
var data = await _http.GetStringAsync(url).ConfigureAwait(false);

// UI code: needed on the UI thread — resume wherever, keep the UI responsive
var data = await LoadAsync().ConfigureAwait(false);
```

**Say in the interview:** "In application code on ASP.NET Core I don't bother — there's no context to capture. In library code I use `ConfigureAwait(false)` on every await, because I don't know who will call me, and it's cheap insurance against deadlocks in UI and legacy ASP.NET callers."

**Listening for:** context capture, the ASP.NET Core no-op, library-vs-app distinction.
