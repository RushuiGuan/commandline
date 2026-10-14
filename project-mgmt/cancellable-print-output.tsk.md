---
name: Ctrl+C is ignored while Print<T> renders a large payload
status: deferred
created: 2026-09-14T05:13:25-04:00
priority: normal
tags: outputs cancellation print
---

## Objective

`Albatross.CommandLine.Outputs.Extensions.Print<T>` is a fully synchronous render
pipeline that takes no `CancellationToken`. Once a command handler calls it with a
large payload, Ctrl+C has no visible effect: System.CommandLine's process-termination
handler suppresses the OS default kill and cancels the token, but nothing inside
`Print` observes a token, so the process appears frozen until the render completes.
`CommandHost.Build()` raises `ProcessTerminationTimeout` to 30 seconds
(`Albatross.CommandLine/CommandHost.cs:149`, up from the 2-second default), which
widens that unresponsive window considerably.

The output surface is a consumed contract for humans, scripts, and agent tooling — a
command that cannot be interrupted while producing that output is a usability defect
in the contract. Make the render surface cancellation-aware so Ctrl+C interrupts a
large print within a short, bounded time.

## Design

- **Thread a `CancellationToken` through the render surface.** Add it as a final
  optional parameter on `Print<T>` and on the four wrappers (`PrintSuccess`,
  `PrintSuccessWithData`, `PrintError`, `PrintErrorWithData`). Optional-with-default
  keeps every existing call site source-compatible and preserves today's behavior when
  no token is supplied. Handlers already have a token in hand — `BaseHandler<T>`
  implements `IAsyncCommandHandler.InvokeAsync(CancellationToken)` — so passing one is
  a one-argument change at the call site.

- **Signal cancellation by throwing `OperationCanceledException` out of `Print`.** No
  new error path is needed: `GlobalCommandAction.InvokeAsync` already catches it, logs
  a warning, and routes an `Error(ErrorSource.CommandTaskCancellation, …)` through
  `ICommandErrorHandler`. This follows the *unified error contract* decision in
  `project.md` (every outcome is an `Error` carrying its `Source`; the handler, not a
  reserved exit code, explains it).

- **Accept partial stdout.** Cancelling mid-render leaves truncated JSON on stdout
  while the cancellation notice goes to stderr. That is the correct trade: the user
  asked to stop, and a consumer of a cancelled command must check the exit code
  anyway. The alternative — buffer the whole rendered output and write it atomically —
  is essentially the current behavior, and it is the bug.

- **In-stage checks are required; between-stage checks are not enough.** `Print` has
  four stages that can each stall independently on a large payload: (1) Newtonsoft
  serialize into a `StringWriter`, (2) re-parse the text into a `JToken` (the
  round-trip that exists to coerce Guid/DateTime scalars for JmesPath), (3) the
  optional JmesPath transform, (4) the Spectre/`Console.Out` render. A single stage is
  itself the stall, so checking the token only at stage boundaries would still block.

- **Likely mechanism:** a cancellation-checking `TextWriter`/`TextReader` wrapper for
  stages 1–2 (throw from `Write`/`Read` every N calls, cheap and localized) plus
  chunked writing for stage 4. Caveat found while scoping: Spectre's `JsonText` parses
  its input and builds the full segment list *before* writing anything, so wrapping the
  output writer does not cover its internal render. Options for stage 4, to be decided
  by the executor: write the JSON in chunks with a token check between them, or
  introduce a size threshold above which the pretty path degrades to chunked raw
  writes. The JmesPath transform (stage 3) is opaque third-party work and may not be
  interruptible at all — bounding it may mean accepting it as a known gap.

## Implementation

Affected files:
- `Albatross.CommandLine.Outputs/Extensions.cs` — `Print<T>` is the single choke point;
  `PrintSuccess`, `PrintSuccessWithData`, `PrintError`, and `PrintErrorWithData` all
  funnel through it, so the token has to reach it from each of them.
- `Albatross.CommandLine.Outputs/GlobalErrorHandler.cs:42` — prints the error output
  with no token in scope. Error payloads are small; leave it passing the default.
- `Sample.CommandLine/ShowConfig.cs:28` — the only in-repo call site of `Print`. Useful
  as the repro vehicle: print a large array (hundreds of thousands of elements) on a
  TTY through the pretty path and press Ctrl+C during the render.

Existing machinery to build on, not rebuild:
- `Albatross.CommandLine/GlobalCommandAction.cs:62` already converts
  `OperationCanceledException` into `ErrorSource.CommandTaskCancellation` and logs it,
  so a cancelled `Print` produces a clean, contract-shaped stderr result for free.
- `Albatross.CommandLine/CommandHost.cs:149` sets `ProcessTerminationTimeout` to 30s
  deliberately, so handlers doing real I/O can unwind. Keep it — but note it is the
  reason non-cooperative synchronous work looks frozen for so long, and it means the
  fix has to come from `Print` cooperating, not from shortening the grace period.

Do not conflate with `scommandline-preaction-cancellation-bug.md` in this folder: that
is a separate upstream System.CommandLine defect where Ctrl+C during an *async
pre-action* is not cancellable at all (no termination handler is installed for that
phase). This task is about cooperative cancellation inside the command action.

Open question: confirm what System.CommandLine's `ProcessTerminationHandler` does once
`ProcessTerminationTimeout` elapses on Windows — force-terminate the process, or return
and let the invocation finish. That decides whether the *old* symptom was "frozen 30s,
then exits" or "frozen until the render completes". It does not change the fix, which
makes `Print` cooperate long before the grace period matters. The System.CommandLine
source is not cloned on this machine, so this needs the upstream repository — do not
decompile the assembly.

### What execution settled

- **Signature is breaking, by design** (prerelease, agreed with the maintainer):
  `Print<T>(this T value, JmesPathExpression? expression, bool compact,
  CancellationToken cancellationToken, bool stderr = false)`. The token is required and
  sits *before* `stderr` so `stderr` keeps its default — C# forbids a required parameter
  after an optional one, and the common call stays `Print(query, compact, token)`.
- **The token is required only where the payload is unbounded.** `PrintSuccessWithData`
  and `PrintErrorWithData` take it (arbitrary `Data`); `PrintSuccess` and `PrintError`
  render a small fixed envelope and keep their signatures, passing
  `CancellationToken.None` internally. `GlobalErrorHandler` does the same deliberately:
  it runs *after* cancellation, so the handler's token is already cancelled and would
  abort the report of the cancellation itself.
- **Cancellation rides on the output buffering, in one wrapper.** Internal
  `CancellationTextWriter` buffers into 64KB blocks before forwarding to the real writer
  and checks the token at each block boundary. Buffering is not an optimization bolted
  on: measured on 100k records, writing indented JSON token-by-token into `Console.Out`
  (which auto-flushes, so every token costs a syscall) took 6,607ms against 220ms for
  the same output written in blocks. The block boundary is then the natural cancellation
  checkpoint, so one mechanism serves both.
- **Buffer at the text level, never by opening the raw stdout handle.** An earlier
  attempt wrapped `Console.OpenStandardOutput()` in a `StreamWriter`; that bypasses
  `Console.SetOut`, so any host or test redirecting output silently loses the payload.
  The unit test caught it.
- **The re-parse stage is not guarded** (maintainer's call). A `CancellationTextReader`
  wrapper was written and then removed as unnecessary machinery. The cost is measured:
  at 500k records the re-parse is ~4.9s, so Ctrl+C can sit dead for that long on a large
  payload. The token is checked on either side of the stage.
- **Spectre is bounded by size, not made interruptible.** `Extensions.PrettyPrintThreshold`
  (default 512KB, measured on the minified JSON, settable by the consumer) decides:
  at or below it the colored `JsonText` render is used unchanged; above it the
  non-compact path writes `Formatting.Indented` JSON through the blocking writer —
  indentation preserved, color given up, fully interruptible. Measuring the threshold
  costs one minified string that the old code built anyway. Spectre's cost is the reason
  the threshold exists at all: rendering 500k records (64.7MB) took 335,690ms against
  ~7s for the whole non-Spectre path.
- **The JmesPath transform remains a known gap.** `JmesPath.Net` exposes no cancellation
  hook, so the transform is checked on entry and exit but cannot be interrupted mid-run.

### Why the task is deferred

Spectre.Console has no cancellation-aware print API. `JsonText` is a renderable: the
console parses the whole document and builds every segment before it writes the first
character, and no overload accepts a `CancellationToken`. There is no seam to check one
from, so a Spectre render simply cannot be interrupted — whatever cancellation machinery
surrounds it, the process stays unresponsive until the render finishes.

Moving off Spectre makes cancellation work — the payload can be written incrementally with
a token check between blocks — but the cost is color, which is a stated feature of the
output contract ("color is free and non-destructive", rendered for a TTY and stripped when
piped). Trading the feature away to fix the symptom is not worth it.

So the task is deferred. It becomes actionable again if an alternative appears that can
render colored JSON *and* be interrupted — a syntax-highlighting writer that emits ANSI
incrementally rather than building the whole document first, whether a third-party library
or a small in-house tokenizer over the JSON text.

## Conclusion

**Deferred** (2026-09-14), not abandoned — see *Why the task is deferred* above. Reopen if
a cancellable colored-JSON renderer turns up.

The work done before deferral is on branch `cancellable-print-output` and is left there
rather than merged.

- `Albatross.CommandLine.Outputs/Extensions.cs` — new `Print<T>` signature, token
  observed across all four stages, `PrettyPrintThreshold`, `ToJsonText` helper, and the
  wrapper helpers updated per the rule above.
- `Albatross.CommandLine.Outputs/CancellationTextWriter.cs`,
  `CancellationTextReader.cs` — new internal wrappers.
- `Albatross.CommandLine.Outputs/GlobalErrorHandler.cs`, `Sample.CommandLine/ShowConfig.cs`
  — call sites updated; the README gained the feature bullet and the token in its sample.
- `Sample.CommandLine/TestLargeOutput.cs` — new `test large-output --count N` command as
  the manual Ctrl+C repro vehicle (the original plan reused `ShowConfig`, whose payload
  is far too small).
- `Albatross.CommandLine.Test/Extensions_Print.cs` — covers a pre-cancelled token
  throwing before any output is written, an uncancelled token printing complete JSON,
  and the over-threshold path producing complete indented JSON.

Verified: all 90 tests in `Albatross.CommandLine.Test` pass; `Sample.CommandLine` builds
and `test large-output` prints correctly.

What that branch does *not* solve is the case the task exists for: a colored render on a
TTY. It only stays interruptible by abandoning color above `PrettyPrintThreshold`, which
is the trade the deferral rejects.
