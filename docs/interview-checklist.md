# Interview Checklist — how to use this repo

## The 30-second answer structure

Every answer in this repo follows the same shape — steal it for the interview:

1. **The one-liner** — the direct answer, first sentence. ("Strings are immutable; `StringBuilder` is for loops.")
2. **The why** — the mechanism behind it. (Allocations, the state machine, the consistency rule.)
3. **The story** — a time you hit it, or a concrete failure it causes. (The production bug, the flaky test, the 3 a.m. page.)
4. **The trade-off** — what you'd give up, and when you'd choose the other option.

Interviewers score the why and the trade-off far more than the one-liner.

## The week before the interview

| Day | Do this |
|---|---|
| 7–5 days out | Read one doc file per day. Run its demos. Say each answer out loud once. |
| 4–2 days out | `dotnet test` — read the test names; each one is a claim you should be able to defend. Re-read the questions you stumbled on. |
| 1 day out | Read this checklist + the system-design closer (Q50). Sleep. |
| Day of | Skim the question index in the README. Don't cram new material. |

## Red flags — things that lose senior interviews

- Answering with a definition and stopping there (no why, no trade-off, no story).
- "I would use X" with no sense of what X costs.
- Claiming a skill you can't demonstrate — every claim here has runnable code; yours should too.
- Blocking on async code (`.Result`) and not knowing why it's a problem.
- Designing distributed systems before a modular monolith earns its split.
- Saying "it depends" without then saying what it depends *on*.

## Green flags — things that win them

- Naming what breaks first in a design, unprompted.
- Measuring before optimising — and saying so.
- Admitting the gap: "I haven't used that in production; here's how I'd reason about it."
- A story with numbers: latency before/after, allocation counts, incident duration.
- Asking a clarifying question before designing (Q50 is the test of this).

## In the room: tactics

- **Think out loud.** The interviewer is scoring your reasoning, not your silence.
- **Write the code on the board, then run it mentally.** Off-by-one in a demo is fine; hand-waving the mechanism is not.
- **If you don't know, say so in one sentence, then reason from first principles.** A good wrong answer with clear reasoning beats a memorised right one.
- **End system design with trade-offs and failure modes**, not with a diagram.
