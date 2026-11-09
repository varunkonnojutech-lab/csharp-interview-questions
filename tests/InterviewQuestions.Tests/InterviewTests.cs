// Tests that PROVE the interview answers in docs/01-csharp-language.md.
// Every test here is a claim from a Q&A, verified by running code.
using InterviewQuestions.Demos;

namespace InterviewQuestions.Tests;

public class LanguageTests
{
    [Fact]
    public void ClosureCapture_BuggyVersion_CapturesFinalValue()
    {
        // Q1: a lambda captures the VARIABLE, not its value.
        var result = ClosureCapture.Buggy();
        Assert.Equal(new[] { 5, 5, 5, 5, 5 }, result);
    }

    [Fact]
    public void ClosureCapture_FixedVersion_CapturesEachValue()
    {
        var result = ClosureCapture.Fixed();
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, result);
    }

    [Fact]
    public void DeferredExecution_QueryRunsOnEveryEnumeration()
    {
        // Q (LINQ): queries are lazy and re-run on every enumeration.
        var (afterFirst, total, result) = DeferredExecution.RunDemo();
        Assert.Equal(new[] { 2, 4 }, result);
        Assert.Equal(5, afterFirst);  // 5 source items filtered once
        Assert.Equal(10, total);      // ...and again on the second ToList()
    }

    [Fact]
    public void RecordEquality_RecordsAreValueEqual_ClassesAreReferenceEqual()
    {
        // Q5: records compare by value, classes by reference.
        var r1 = new RecordEquality.PersonRecord("Asha", 30);
        var r2 = new RecordEquality.PersonRecord("Asha", 30);
        Assert.True(r1 == r2);
        Assert.Equal(r1.GetHashCode(), r2.GetHashCode());

        var c1 = new RecordEquality.PersonClass("Asha", 30);
        var c2 = new RecordEquality.PersonClass("Asha", 30);
        Assert.False(c1 == c2);
        Assert.Equal("Asha", c1.Name);
    }

    [Fact]
    public void Span_SlicingAndParsing_WorkWithoutAllocatingStrings()
    {
        // Q19: Span<T> slices existing memory; no new strings are created.
        var (sum, firstFive, parsed) = SpanDemo.RunDemo();
        Assert.Equal(9, sum);          // 2 + 3 + 4
        Assert.Equal("Hello", firstFive);
        Assert.Equal(42, parsed);
    }

    [Fact]
    public void Boxing_TwoBoxedInts_AreDistinctObjects()
    {
        // Q3: every boxing allocates a NEW object on the heap.
        var (distinct, sum) = BoxingDemo.RunDemo();
        Assert.True(distinct);
        Assert.Equal(6, sum);
    }

    [Fact]
    public void StringBuilder_ProducesSameResult_AsConcatenation()
    {
        // Q1: StringBuilder is the loop-safe way to build strings.
        var (concat, built) = StringAndBuilder.RunDemo();
        Assert.Equal("01234", concat);
        Assert.Equal("01234", built);
    }

    [Fact]
    public void Disposal_UsingDisposes_InReverseDeclarationOrder()
    {
        // Q17: `using var` disposes at scope exit, reverse order, even on exceptions.
        var log = DisposalDemo.RunDemo();
        Assert.Equal(new[] { "open outer", "open inner", "work", "dispose inner", "dispose outer" }, log);
    }
}

public class AsyncConcurrencyTests
{
    [Fact]
    public async Task WhenAll_IsFaster_ThanSequentialAwaits()
    {
        // Q12: awaiting in a loop serialises independent work.
        // Relative assertion only: absolute timings are meaningless on a
        // loaded CI runner, but the ratio is stable — parallel work takes
        // ~1/3 of sequential work regardless of machine speed.
        var seq = await AsyncTiming.SequentialAsync();
        var par = await AsyncTiming.ParallelAsync();
        Assert.Equal(new[] { "a", "b", "c" }, seq.order);
        Assert.Equal(new[] { "a", "b", "c" }, par.order);
        Assert.True(seq.ms >= 250,
            $"Three sequential 100 ms steps should take ~300 ms, took {seq.ms} ms");
        Assert.True(par.ms * 2 < seq.ms,
            $"WhenAll ({par.ms} ms) should take under half of sequential ({seq.ms} ms)");
    }

    [Fact]
    public async Task CancellationToken_StopsWork_WhenCancelled()
    {
        // Q13: a token that is checked actually stops the work.
        // ThrowsAnyAsync: Task.Delay throws TaskCanceledException, a SUBCLASS of
        // OperationCanceledException — ThrowsAsync demands an exact type match.
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(55));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CancellationDemo.CountToAsync(1000, cts.Token));
    }

    [Fact]
    public async Task ConcurrentDictionary_ParallelIncrements_AreExact()
    {
        // Q41: shared counters need a thread-safe type, not a plain int.
        int total = await ConcurrencyDemo.ParallelCounterAsync();
        Assert.Equal(10_000, total);
    }

    [Fact]
    public async Task SemaphoreSlim_NeverExceeds_ItsLimit()
    {
        // Q40: bound the work, don't hope it stays small.
        int peak = await ConcurrencyDemo.ThrottledAsync();
        Assert.Equal(2, peak);
    }

    [Fact]
    public async Task Channel_DeliversAllItems_InOrder()
    {
        // Q44: channels are the producer/consumer primitive.
        var consumed = await ChannelsDemo.RunDemoAsync();
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, consumed);
    }
}
