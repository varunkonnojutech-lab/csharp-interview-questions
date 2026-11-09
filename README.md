# 50 Senior .NET Interview Questions — with runnable code

[![Build](https://github.com/varunkonnojutech-lab/csharp-interview-questions/actions/workflows/ci.yml/badge.svg)](https://github.com/varunkonnojutech-lab/csharp-interview-questions/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/varunkonnojutech-lab/csharp-interview-questions)

Fifty questions a senior .NET developer actually gets asked — each answered in plain words, with the code that proves it. Every claim in this repo runs: the demos show the behaviour, and the xUnit tests assert it. No memorised definitions, no answers you couldn't defend on a whiteboard.

## Who is this for

.NET developers preparing for senior interviews, and anyone who has ever answered a question correctly and then been asked "…and why?" and gone quiet. If you can run the code and explain the trade-off, you can answer the question.

## What you'll learn

- **C# language** — the traps (closure capture, boxing, equality) and when each feature earns its keep
- **Async/await** — what the compiler actually does, deadlocks, cancellation, `WhenAll` vs loops
- **Runtime & memory** — GC generations, leaks that aren't leaks, `Span<T>` and zero-allocation code
- **ASP.NET Core** — pipeline order, DI lifetimes, JWT + refresh tokens, versioning, performance
- **EF Core & data** — N+1, change tracking, concurrency, migrations, indexing
- **Architecture** — Clean vs vertical slices, aggregates, outbox, sagas, monolith vs microservices
- **Concurrency** — locks vs semaphores, races, deadlocks, channels, thread-pool starvation
- **Azure & DevOps** — where to deploy, keeping secrets out of git, CI/CD, observability
- **System design** — the URL-shortener closer, and how to structure any design answer

## Quick start

You need .NET 8. Nothing else.

```bash
dotnet test   # 13 tests: every runnable claim in the repo, proven by code
dotnet run --project src/InterviewQuestions   # run all 11 demos
dotnet run --project src/InterviewQuestions -- closure   # run one demo by name
```

## How to use this repo

1. **Read a question, then run its demo.** The demo prints the behaviour and the *why* — don't just read the answer.
2. **Read the test that proves it.** Test names in `tests/InterviewQuestions.Tests/` are the claims; if you can't defend a claim, re-read the question.
3. **Say the answer out loud** using the 30-second structure in [docs/interview-checklist.md](docs/interview-checklist.md): one-liner → why → story → trade-off.
4. **Do the follow-ups.** Every question ends with what the interviewer asks next.

## The 50 questions

### C# language — [docs/01-csharp-language.md](docs/01-csharp-language.md)

| # | Question |
|---|---|
| 1 | What's the difference between `string` and `StringBuilder`, and when does it matter? |
| 2 | Explain value types vs reference types — where do they live, and what changes when you pass them? |
| 3 | What is boxing and unboxing, and why should you care? |
| 4 | `var` vs explicit types; `dynamic` vs `object` — when would you use each? |
| 5 | What are records, and how is record equality different from class equality? |
| 6 | Explain `ref`, `out`, `in`, and `ref struct` (Span\<T\>). |
| 7 | What's the difference between `==` and `Equals()`? What must you do when you override one? |

### Async / await — [docs/02-async-await.md](docs/02-async-await.md)

| # | Question |
|---|---|
| 8 | What happens when you `await` a Task — what does the compiler generate? |
| 9 | Why is `async void` dangerous, and where is it allowed? |
| 10 | What's the difference between `Task`, `ValueTask`, and `Task<T>`? |
| 11 | Why does `.Result` / `.Wait()` deadlock, and what changed in ASP.NET Core? |
| 12 | `Task.WhenAll` vs `Task.WhenAny` vs `await` in a loop — which and when? |
| 13 | How do you cancel async work properly? |
| 14 | What is `ConfigureAwait(false)` and when do you still need it? |

### Runtime, GC & memory — [docs/03-runtime-gc-memory.md](docs/03-runtime-gc-memory.md)

| # | Question |
|---|---|
| 15 | How does the .NET garbage collector work — generations, LOH, compaction? |
| 16 | What is a memory leak in .NET, and how do you find one? |
| 17 | What is `IDisposable`, and what's the difference between `using`, `using var`, and finalizers? |
| 18 | What is string interning, and how does `string` equality actually work? |
| 19 | How do `Span<T>` and `Memory<T>` avoid allocations? |
| 20 | What causes Gen 2 GC pressure, and how do you reduce it? |

### ASP.NET Core & APIs — [docs/04-aspnet-core.md](docs/04-aspnet-core.md)

| # | Question |
|---|---|
| 21 | How does the ASP.NET Core request pipeline work — and why does middleware order matter? |
| 22 | What are the DI lifetimes — Singleton, Scoped, Transient — and what's the captive dependency trap? |
| 23 | How do you handle errors and return consistent API responses? |
| 24 | How do you secure an API — JWT, refresh tokens, RBAC? |
| 25 | Minimal APIs vs controllers — when do you choose which? |
| 26 | How do you version an API without breaking clients? |
| 27 | How do you make an API fast — what do you measure first? |

### EF Core & data — [docs/05-efcore-data.md](docs/05-efcore-data.md)

| # | Question |
|---|---|
| 28 | How do you avoid the N+1 query problem? |
| 29 | When do you use EF Core vs Dapper vs raw SQL? |
| 30 | What is change tracking, and when do you turn it off? |
| 31 | How do you handle concurrency conflicts? |
| 32 | Migrations in production — how do you deploy schema changes safely? |
| 33 | How do you design an index, and how do you know a query is slow? |

### Architecture & design — [docs/06-architecture-design.md](docs/06-architecture-design.md)

| # | Question |
|---|---|
| 34 | Clean Architecture vs vertical slices — which and when? |
| 35 | How do you design a microservice boundary (DDD aggregates)? |
| 36 | How do you keep services consistent without distributed transactions? |
| 37 | How do you handle failures between services? |
| 38 | Monolith vs microservices — how do you decide? |
| 39 | How do you design an API that other teams can actually use? |

### Concurrency & threading — [docs/07-concurrency.md](docs/07-concurrency.md)

| # | Question |
|---|---|
| 40 | `lock` vs `SemaphoreSlim` vs `Monitor` vs `Mutex` — which and when? |
| 41 | How do you write thread-safe code without locking everything? |
| 42 | What's a race condition and a deadlock, and how do you diagnose each? |
| 43 | When do you use `Parallel.ForEach`, PLINQ, `Task`, or channels? |
| 44 | How do channels (`System.Threading.Channels`) solve producer/consumer? |
| 45 | What is thread-pool starvation and how do you spot it? |

### Azure, DevOps & system design — [docs/08-azure-devops.md](docs/08-azure-devops.md)

| # | Question |
|---|---|
| 46 | How do you deploy a .NET app to Azure — App Service vs AKS vs Functions? |
| 47 | How do you keep secrets out of source code? |
| 48 | What does a good CI/CD pipeline for .NET look like? |
| 49 | How do you observe a production .NET app? |
| 50 | Walk me through how you'd design a URL shortener. |

## The demos

Every demo answers one question with code you can run and break:

| Demo | Question it proves | Run it |
|---|---|---|
| `closure` | Q1 — lambdas capture the variable, not the value | `dotnet run -- closure` |
| `deferred` | LINQ queries re-run on every enumeration | `dotnet run -- deferred` |
| `records` | Q5 — record vs class equality | `dotnet run -- records` |
| `span` | Q19 — zero-allocation slicing and parsing | `dotnet run -- span` |
| `boxing` | Q3 — boxing allocates a new object every time | `dotnet run -- boxing` |
| `strings` | Q1 — `string +=` in a loop vs `StringBuilder` | `dotnet run -- strings` |
| `async` | Q12 — `WhenAll` vs sequential awaits (~100 ms vs ~300 ms) | `dotnet run -- async` |
| `cancel` | Q13 — cooperative cancellation with a token | `dotnet run -- cancel` |
| `concurrency` | Q40–Q41 — `ConcurrentDictionary` + `SemaphoreSlim` | `dotnet run -- concurrency` |
| `channels` | Q44 — producer/consumer with back-pressure | `dotnet run -- channels` |
| `disposal` | Q17 — `using var` disposes in reverse order | `dotnet run -- disposal` |

## How it works

The answers live in `docs/` — one file per topic, each question with the answer, a code snippet, what the interviewer is listening for, and the follow-up they ask next. The demos live in `src/InterviewQuestions/Demos/`, and the tests in `tests/InterviewQuestions.Tests/` assert the exact behaviour each answer claims (closure capture returns `5, 5, 5, 5, 5`; `WhenAll` beats sequential awaits; the channel delivers in order).

The one mistake to avoid: memorising the one-liner and stopping there. Interviewers score the *why* and the *trade-off* — that's why every answer here has both, and why the checklist in [docs/interview-checklist.md](docs/interview-checklist.md) exists.

## Next steps

- Work through the [interview checklist](docs/interview-checklist.md) — the 30-second answer structure and the week-before plan
- Go deeper where you're weakest: [jwt-auth-deepdive](https://github.com/varunkonnojutech-lab/jwt-auth-deepdive) (Q24), [dotnet-diagnostics](https://github.com/varunkonnojutech-lab/dotnet-diagnostics) (Q16, Q45), [resilience-with-polly](https://github.com/varunkonnojutech-lab/resilience-with-polly) (Q37), [outbox-pattern](https://github.com/varunkonnojutech-lab/outbox-pattern) + [saga-choreography](https://github.com/varunkonnojutech-lab/saga-choreography) (Q36)
- Got a question this repo should cover? Open an issue — the best interview questions come from real interviews
