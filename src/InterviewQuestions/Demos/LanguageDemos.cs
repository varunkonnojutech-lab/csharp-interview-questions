// Demo for Q1 (closure capture) and Q7 (== vs Equals) support code.
// Run: dotnet run -- closure
using System.Collections.Concurrent;

namespace InterviewQuestions.Demos;

public static class ClosureCapture
{
    // THE classic interview trap: every lambda captures the SAME loop variable.
    public static List<int> Buggy()
    {
        var actions = new List<Func<int>>();
        for (int i = 0; i < 5; i++)
            actions.Add(() => i);          // captures `i` by reference
        return actions.Select(a => a()).ToList(); // 5, 5, 5, 5, 5
    }

    // THE fix: copy into a fresh variable inside the loop body.
    public static List<int> Fixed()
    {
        var actions = new List<Func<int>>();
        for (int i = 0; i < 5; i++)
        {
            int captured = i;              // new variable per iteration
            actions.Add(() => captured);
        }
        return actions.Select(a => a()).ToList(); // 0, 1, 2, 3, 4
    }

    public static void Run()
    {
        Console.WriteLine("Buggy : " + string.Join(", ", Buggy()));
        Console.WriteLine("Fixed : " + string.Join(", ", Fixed()));
        Console.WriteLine("Why: a lambda captures the variable, not its value. " +
            "In a `for` loop there is ONE variable shared by all iterations; " +
            "copy it into a loop-local variable to get one per iteration.");
    }
}

public static class DeferredExecution
{
    public static (int firstCount, int secondCount, List<int> result) RunDemo()
    {
        int evaluations = 0;
        var source = new[] { 1, 2, 3, 4, 5 };
        var query = source.Where(n => { evaluations++; return n % 2 == 0; });

        var first = query.ToList();   // evaluates here
        int afterFirst = evaluations;
        var second = query.ToList();  // evaluates AGAIN — nothing is cached
        return (afterFirst, evaluations, first);
    }

    public static void Run()
    {
        var (first, second, result) = RunDemo();
        Console.WriteLine($"Even numbers: {string.Join(", ", result)}");
        Console.WriteLine($"Filter ran {first} time(s) for the 1st ToList, {second} total after the 2nd.");
        Console.WriteLine("Why: LINQ queries are lazy. They run when enumerated, " +
            "and re-run on every enumeration. Call ToList() once if you need the result twice.");
    }
}

public static class RecordEquality
{
    public record PersonRecord(string Name, int Age);
    public class PersonClass(string Name, int Age)
    {
        public string Name { get; } = Name;
        public int Age { get; } = Age;
    }

    public static void Run()
    {
        var r1 = new PersonRecord("Asha", 30);
        var r2 = new PersonRecord("Asha", 30);
        var c1 = new PersonClass("Asha", 30);
        var c2 = new PersonClass("Asha", 30);
        Console.WriteLine($"record == record : {r1 == r2}   (value equality)");
        Console.WriteLine($"class  == class  : {c1 == c2}   (reference equality)");
        Console.WriteLine($"record.Equals     : {r1.Equals(r2)}");
        Console.WriteLine($"record.GetHashCode equal: {r1.GetHashCode() == r2.GetHashCode()}");
        Console.WriteLine("Why: records are value-equal by default and synthesize " +
            "Equals/GetHashCode for you. Classes compare references unless you override both.");
    }
}

public static class SpanDemo
{
    public static (int sum, string firstThree, int parsed) RunDemo()
    {
        // Zero-allocation slicing: no new strings, no new arrays.
        Span<int> numbers = stackalloc int[] { 1, 2, 3, 4, 5 };
        int sum = 0;
        foreach (int n in numbers[1..4]) sum += n;   // 2 + 3 + 4 = 9

        ReadOnlySpan<char> text = "Hello, World!".AsSpan();
        string firstThree = new string(text[..5]);                    // "Hello"

        ReadOnlySpan<char> digits = "42".AsSpan();
        int.TryParse(digits, out int parsed);                  // parses without allocating a string
        return (sum, firstThree, parsed);
    }

    public static void Run()
    {
        var (sum, firstThree, parsed) = RunDemo();
        Console.WriteLine($"Span slice 1..4 summed: {sum}");
        Console.WriteLine($"First 5 chars via Span: \"{firstThree}\"");
        Console.WriteLine($"Parsed \"42\" from a Span: {parsed}");
        Console.WriteLine("Why: Span<T> is a ref struct over existing memory — slicing " +
            "costs nothing. Use it in hot paths (parsing, buffers) to stop Gen 0 churn.");
    }
}

public static class BoxingDemo
{
    public static (bool boxedAreDistinct, int genericSum) RunDemo()
    {
        object a = 5;   // boxes: allocates an object on the heap
        object b = 5;   // boxes again — a DIFFERENT object
        bool distinct = !ReferenceEquals(a, b);

        // Generics avoid boxing entirely: List<int> stores raw ints.
        var numbers = new List<int> { 1, 2, 3 };
        int sum = 0;
        foreach (int n in numbers) sum += n;
        return (distinct, sum);
    }

    public static void Run()
    {
        var (distinct, sum) = RunDemo();
        Console.WriteLine($"Two boxed 5s are distinct objects: {distinct}");
        Console.WriteLine($"List<int> sum (no boxing): {sum}");
        Console.WriteLine("Why: boxing copies a value type onto the heap. " +
            "It is invisible in code review — ArrayList and string.Format with ints do it silently.");
    }
}

public static class StringAndBuilder
{
    public static (string concat, string built) RunDemo()
    {
        string concat = "";
        for (int i = 0; i < 5; i++) concat += i;      // 5 throwaway strings

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 5; i++) sb.Append(i);
        return (concat, sb.ToString());
    }

    public static void Run()
    {
        var (concat, built) = RunDemo();
        Console.WriteLine($"string += : \"{concat}\"  (5 intermediate strings allocated)");
        Console.WriteLine($"StringBuilder: \"{built}\" (one buffer, grown in place)");
        Console.WriteLine($"Interned literal equality: {ReferenceEquals("hello", "hello")} " +
            "(literals are interned; never rely on ReferenceEquals for strings)");
        Console.WriteLine("Why: strings are immutable — every + allocates. " +
            "In a loop, use StringBuilder; for one or two joins, + is fine and clearer.");
    }
}
