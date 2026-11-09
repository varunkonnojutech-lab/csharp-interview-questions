# C# Language — Q1–Q7

## Q1. What's the difference between `string` and `StringBuilder`, and when does it matter?

Strings in C# are **immutable** — every `+` creates a brand-new string and throws the old one away. In a loop that means O(n²) allocations. `StringBuilder` keeps one mutable buffer and grows it in place.

```csharp
// 10,000 throwaway strings — slow, Gen 0 churn
string s = "";
for (int i = 0; i < 10_000; i++) s += i;

// One buffer, grown in place — the right tool in a loop
var sb = new StringBuilder();
for (int i = 0; i < 10_000; i++) sb.Append(i);
```

**Say in the interview:** "For one or two joins, `+` is fine and clearer. In a loop or when building a large payload, `StringBuilder` — and in .NET 8, string interpolation with `$""` is already optimised by the compiler, so the loop is the real case."

**Listening for:** immutability, allocation cost, knowing when *not* to over-optimise.
**Follow-up:** "What about `string.Create` or `Span<char>` for zero-allocation formatting?" → see Q19.

> ▶ Runnable: `dotnet run -- strings` · proven by `StringBuilder_ProducesSameResult_AsConcatenation`

## Q2. Explain value types vs reference types — where do they live, and what changes when you pass them?

Value types (`int`, `struct`, `DateTime`) hold their data directly; copying one copies the value. Reference types (`class`, `string`, arrays) hold a pointer to data on the heap; copying one copies the pointer, so two variables see the same object.

```csharp
int a = 5; int b = a; b = 6;      // a is still 5
var p1 = new Person("Asha"); var p2 = p1; p2.Name = "Ben"; // p1.Name is now "Ben"
```

**Say in the interview:** "The rule of thumb: small, immutable, logically-a-value data → struct; identity and shared mutable state → class. And `where` they live is a detail — locals can live on the heap when captured by a closure, and structs can live on the heap inside a class. What matters is copy semantics, not the stack/heap slogan."

**Listening for:** copy semantics, the closure/heap caveat, struct-vs-class judgement.
**Follow-up:** "When is a struct the wrong choice?" → large structs (over ~16 bytes) get expensive to copy; mutable structs are a bug factory.

## Q3. What is boxing and unboxing, and why should you care?

Boxing wraps a value type in an object on the heap so it can be treated as `object`. Unboxing copies it back out. It is silent — the compiler never warns you.

```csharp
object o = 5;        // boxes: allocates
int n = (int)o;      // unboxes: copies back
var list = new ArrayList { 1, 2, 3 }; // every int boxed — use List<int> instead
```

**Say in the interview:** "Boxing shows up in hot paths — non-generic collections, `string.Format` with value types, and calling `GetType()` or `Equals(object)` on a struct. Generics exist largely to make it go away. In a profile, boxing looks like Gen 0 allocations with no obvious `new`."

**Listening for:** the hidden-allocation angle, generics as the fix.
**Follow-up:** "How do you find boxing in a real app?" → allocation profiling with dotnet-trace / PerfView; see [dotnet-diagnostics](https://github.com/varunkonnojutech-lab/dotnet-diagnostics).

> ▶ Runnable: `dotnet run -- boxing` · proven by `Boxing_TwoBoxedInts_AreDistinctObjects`

## Q4. `var` vs explicit types; `dynamic` vs `object` — when would you use each?

`var` is compile-time type inference — the type is fixed, the compiler just writes it for you. `dynamic` defers binding to runtime: no IntelliSense, no compile-time checking, and a runtime exception if the member doesn't exist.

```csharp
var order = new Order();        // still an Order — use var when the type is obvious
dynamic payload = GetJson();    // resolves at runtime — useful for interop, dangerous as a habit
object anything = 5;            // needs a cast to use as an int
```

**Say in the interview:** "I use `var` when the right-hand side makes the type obvious, explicit types when it doesn't. I reach for `dynamic` only at boundaries — COM interop, untyped JSON, dynamic proxies — and I isolate it behind a typed wrapper so it doesn't leak."

**Listening for:** knowing `var` is not dynamic, and restraint around `dynamic`.

## Q5. What are records, and how is record equality different from class equality?

A `record` is a class (or struct) with value-based equality synthesized for you: two records are equal when all their properties are equal. Classes compare by reference unless you override `Equals` and `GetHashCode` yourself.

```csharp
public record PersonRecord(string Name, int Age);
var a = new PersonRecord("Asha", 30);
var b = new PersonRecord("Asha", 30);
a == b; // true — value equality, plus a free GetHashCode that matches
```

**Say in the interview:** "Records are my default for DTOs, messages, and anything that is data rather than identity — value equality, immutability with `init`, and `with` expressions for non-destructive mutation. If it has behaviour and a lifecycle, it's a class."

**Listening for:** value vs reference equality, `with`, the DTO use case.
**Follow-up:** "What must stay in sync when you override Equals?" → GetHashCode, and `==` if you overload it.

> ▶ Runnable: `dotnet run -- records` · proven by `RecordEquality_RecordsAreValueEqual_ClassesAreReferenceEqual`

## Q6. Explain `ref`, `out`, `in`, and `ref struct` (Span<T>).

`ref` passes a variable by reference so the method can read and write the caller's storage. `out` is the same but the method *must* assign it — the classic "return two things" pattern. `in` passes by reference but read-only, for large structs you don't want to copy. A `ref struct` (like `Span<T>`) can only live on the stack, so it can never be boxed or stored on the heap.

```csharp
void Swap(ref int a, ref int b) => (a, b) = (b, a);
bool TryParse(string s, out int value) { ... } // out: must assign before returning
void Print(in BigStruct s) { ... }            // in: no copy, no mutation
Span<int> slice = numbers.AsSpan()[1..4];    // ref struct — stack only
```

**Say in the interview:** "I use `out` for Try-patterns, `ref` rarely and only for hot-path mutation, and `in` for large readonly structs. `Span<T>` being a ref struct is why slicing is allocation-free — the compiler guarantees it never escapes to the heap."

**Listening for:** the Try-pattern, the stack-only guarantee of ref structs.

## Q7. What's the difference between `==` and `Equals()`? What must you do when you override one?

`==` is an operator — for reference types it defaults to reference equality, for strings it's overloaded to value equality. `Equals()` is a virtual method, also reference equality by default. The trap: overriding one without the other (and without `GetHashCode`) breaks dictionaries and sets.

```csharp
public class Money : IEquatable<Money>
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "";
    public override bool Equals(object? obj) => obj is Money m && Equals(m);
    public bool Equals(Money? other) => other is not null && Amount == other.Amount && Currency == other.Currency;
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public static bool operator ==(Money? a, Money? b) => Equals(a, b);
    public static bool operator !=(Money? a, Money? b) => !Equals(a, b);
}
```

**Say in the interview:** "The rule is all-or-nothing: override `Equals`, `GetHashCode`, `==` and `!=` together, or use a record and let the compiler do it. The bug this causes in production is an object that 'exists' but can't be found in a `Dictionary` because its hash changed after insertion."

**Listening for:** the GetHashCode contract, the Dictionary failure mode, records as the shortcut.
