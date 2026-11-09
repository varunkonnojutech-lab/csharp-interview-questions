// Demos for the async/await and concurrency questions (Q8–Q14, Q40–Q45).
// Run: dotnet run -- async | dotnet run -- concurrency
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace InterviewQuestions.Demos;

public static class AsyncTiming
{
    private static async Task<string> SlowStepAsync(string name, int ms)
    {
        await Task.Delay(ms);
        return name;
    }

    // Sequential awaits: total time ADDS UP (~300 ms).
    public static async Task<(List<string> order, long ms)> SequentialAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<string>
        {
            await SlowStepAsync("a", 100),
            await SlowStepAsync("b", 100),
            await SlowStepAsync("c", 100),
        };
        sw.Stop();
        return (results, sw.ElapsedMilliseconds);
    }

    // WhenAll: the three steps run TOGETHER (~100 ms).
    public static async Task<(List<string> order, long ms)> ParallelAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = (await Task.WhenAll(
            SlowStepAsync("a", 100),
            SlowStepAsync("b", 100),
            SlowStepAsync("c", 100))).ToList();
        sw.Stop();
        return (results, sw.ElapsedMilliseconds);
    }

    public static async Task RunAsync()
    {
        var seq = await SequentialAsync();
        var par = await ParallelAsync();
        Console.WriteLine($"Sequential awaits: {seq.ms} ms — {string.Join(",", seq.order)}");
        Console.WriteLine($"Task.WhenAll     : {par.ms} ms — {string.Join(",", par.order)}");
        Console.WriteLine("Why: `await` in a loop waits for each call before starting " +
            "the next. Start all the tasks first, then await them together.");
    }
}

public static class CancellationDemo
{
    // A cancellable loop: checks the token, throws OperationCanceledException.
    public static async Task<int> CountToAsync(int target, CancellationToken token)
    {
        int counted = 0;
        for (int i = 0; i < target; i++)
        {
            token.ThrowIfCancellationRequested();
            await Task.Delay(10, token);
            counted++;
        }
        return counted;
    }

    public static async Task RunAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(55));
        try
        {
            int counted = await CountToAsync(1000, cts.Token);
            Console.WriteLine($"Counted to {counted} (no cancellation)");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Cancelled after ~55 ms, as requested — no hang, no leak.");
        }
        Console.WriteLine("Why: pass the token into every await (Delay, HttpClient, EF Core). " +
            "A token that is never checked is decoration, not cancellation.");
    }
}

public static class ConcurrencyDemo
{
    // Thread-safe counter: 10 tasks x 1,000 increments = exactly 10,000.
    public static async Task<int> ParallelCounterAsync()
    {
        var counts = new ConcurrentDictionary<string, int>();
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
                counts.AddOrUpdate("hits", 1, (_, v) => v + 1);
        }));
        await Task.WhenAll(tasks);
        return counts["hits"];
    }

    // Throttle: at most 2 concurrent workers, even with 6 jobs queued.
    public static async Task<int> ThrottledAsync()
    {
        using var gate = new SemaphoreSlim(2);
        int peak = 0, current = 0;
        var tasks = Enumerable.Range(0, 6).Select(async _ =>
        {
            await gate.WaitAsync();
            try
            {
                int now = Interlocked.Increment(ref current);
                RecordPeak(ref peak, now);
                await Task.Delay(20);
                Interlocked.Decrement(ref current);
            }
            finally { gate.Release(); }
        });
        await Task.WhenAll(tasks);
        return peak;
    }

    // Atomic max: a plain Interlocked.Exchange can lose the larger value
    // when two threads race (one reads stale, writes back the smaller max).
    internal static void RecordPeak(ref int peak, int now)
    {
        int observed;
        do
        {
            observed = peak;
            if (now <= observed) return;
        }
        while (Interlocked.CompareExchange(ref peak, now, observed) != observed);
    }

    public static async Task RunAsync()
    {
        Console.WriteLine($"ConcurrentDictionary counter: {await ParallelCounterAsync()} (expected 10000)");
        Console.WriteLine($"SemaphoreSlim(2) peak concurrency: {await ThrottledAsync()} (never above 2)");
        Console.WriteLine("Why: shared mutable state needs a plan — ConcurrentDictionary for " +
            "counters, SemaphoreSlim to bound work, lock only for short critical sections.");
    }
}

public static class ChannelsDemo
{
    // Producer/consumer without a lock in sight: a bounded channel back-pressures.
    public static async Task<List<int>> RunDemoAsync()
    {
        var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(2)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        var producer = Task.Run(async () =>
        {
            for (int i = 1; i <= 5; i++)
                await channel.Writer.WriteAsync(i);
            channel.Writer.Complete();
        });

        var consumed = new List<int>();
        await foreach (int item in channel.Reader.ReadAllAsync())
            consumed.Add(item);

        await producer;
        return consumed;
    }

    public static async Task RunAsync()
    {
        var consumed = await RunDemoAsync();
        Console.WriteLine($"Channel delivered in order: {string.Join(", ", consumed)}");
        Console.WriteLine("Why: channels replace hand-rolled queues + locks + ManualResetEvents. " +
            "Bounded + Wait gives you back-pressure for free.");
    }
}

public static class DisposalDemo
{
    private sealed class TrackedResource : IDisposable
    {
        private readonly List<string> _log;
        private readonly string _name;
        public TrackedResource(List<string> log, string name) { _log = log; _name = name; _log.Add($"open {_name}"); }
        public void Dispose() => _log.Add($"dispose {_name}");
    }

    // `using var` disposes in REVERSE declaration order, at scope exit.
    public static List<string> RunDemo()
    {
        var log = new List<string>();
        {
            using var outer = new TrackedResource(log, "outer");
            using var inner = new TrackedResource(log, "inner");
            log.Add("work");
        }
        return log;
    }

    public static void Run()
    {
        Console.WriteLine(string.Join(" -> ", RunDemo()));
        Console.WriteLine("Why: disposal order is reverse declaration order, and it runs " +
            "even when an exception is thrown — that is the whole point of `using`.");
    }
}
