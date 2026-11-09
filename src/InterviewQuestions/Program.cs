// Demo runner: `dotnet run` runs everything, `dotnet run -- <name>` runs one demo.
//   dotnet run -- closure | async | concurrency | language | channels | disposal
using InterviewQuestions.Demos;

var demos = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
{
    ["closure"]     = () => { ClosureCapture.Run(); return Task.CompletedTask; },
    ["deferred"]    = () => { DeferredExecution.Run(); return Task.CompletedTask; },
    ["records"]     = () => { RecordEquality.Run(); return Task.CompletedTask; },
    ["span"]        = () => { SpanDemo.Run(); return Task.CompletedTask; },
    ["boxing"]      = () => { BoxingDemo.Run(); return Task.CompletedTask; },
    ["strings"]     = () => { StringAndBuilder.Run(); return Task.CompletedTask; },
    ["async"]       = AsyncTiming.RunAsync,
    ["cancel"]      = CancellationDemo.RunAsync,
    ["concurrency"] = ConcurrencyDemo.RunAsync,
    ["channels"]    = ChannelsDemo.RunAsync,
    ["disposal"]    = () => { DisposalDemo.Run(); return Task.CompletedTask; },
};

if (args.Length == 1 && demos.TryGetValue(args[0], out var single))
{
    Console.WriteLine($"--- {args[0]} ---");
    await single();
    return 0;
}

if (args.Length == 1)
{
    Console.WriteLine($"Unknown demo '{args[0]}'. Available: {string.Join(", ", demos.Keys)}");
    return 1;
}

Console.WriteLine("C# Interview Questions — runnable demos");
Console.WriteLine("Each demo answers one interview question with code you can run.");
Console.WriteLine();
foreach (var (name, run) in demos)
{
    Console.WriteLine($"--- {name} ---");
    await run();
    Console.WriteLine();
}
Console.WriteLine("See docs/ for the 50 questions with full answers.");
return 0;
