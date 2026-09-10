---
name: Render parse errors as JSON through the CommandOutput contract
status: new
created: 2026-07-09T16:54:19-04:00
priority: normal
tags: outputs json parse-error system-commandline v9
---

## Objective

Let a CLI emit **parse errors** as JSON through the existing `Albatross.CommandLine.Outputs`
contract (`CommandOutput` / `Print` / shared `Serializer`), instead of `System.CommandLine`'s
default rendering (red error text plus plain-text help to stderr). Successful results and *runtime*
exceptions already go through the JSON envelope (`Print`, `GlobalErrorHandler`); parse-time
failures do not, so a machine/AI/`jq` consumer still receives unstructured text whenever it gets
the command line wrong — exactly when a structured answer is most useful.

Opt-in is **registration-time**: the CLI author registers the handler once at bootstrap, and
thereafter the CLI always prints parse errors as JSON. This is not a per-invocation flag.

> Scope narrowed 2026-08-25. This task originally also covered a `--json-help` flag. That feature
> grew its own requirement (help data for a command *and its whole subcommand subtree*) and now
> lives in `global-json-help-option.tsk.md`. The two share a motivation but nothing else: different
> opt-in models, different packages touched, and they land independently.

## Design

### Where this fits in v9

The v9 output strategy (`project.md` → Key Design Decisions, "formalizes a first-class command
output strategy") makes stdout/stderr a clean, JSON-first contract. `CommandOutput` /
`CommandOutput<T>`, the shared `Serializer`, and the `Print` render surface (`--compact`, JmesPath
`--query`, ANSI stripped when piped) all live in `Albatross.CommandLine.Outputs`.
`GlobalErrorHandler` already renders *runtime* errors as a `CommandOutput` envelope on stderr.

A parse error **is** an error, so it keeps the envelope — see
[the envelope wraps outcomes, not data](project.md#key-design-decisions). This feature belongs in
Outputs (which owns the serializer, the envelope and `Print`) and must be **opt-in**, per the
standing v9 principle "capability over forced policy; never force cross-cutting/global options on
consumers". Core stays JSON-agnostic: it defines the seam only, exactly as it already does for
`ICommandErrorHandler`.

### The core constraint: `ParseResult.Action` is read-only

Verified against the System.CommandLine source, 2026-07-09:

- **`ParseErrorAction`** (`Invocation/ParseErrorAction.cs`) is `sealed`. When parsing produces
  errors the parser sets the result's action to a `ParseErrorAction`, which writes each
  `parseResult.Errors[].Message` to `InvocationConfiguration.Error` in red, invokes the proximate
  `HelpOption`'s action, and returns 1.
- **`ParseResult.Action` is read-only** — `public CommandLineAction? Action => _action ?? CommandResult.Command.Action;`
  over a `private readonly` backing field. So a custom action **cannot** be substituted into the
  parse result after `Parse()`.

That rules out "replace the action" and forces the interception to happen *before* invocation,
which is what shapes the design below.

### The seam

Introduce an interface in **core** symmetric with the existing `ICommandErrorHandler` (which
handles runtime exceptions):

- **Core** (`Albatross.CommandLine`): add `IParseErrorHandler` with `int Handle(ParseResult result)`.
  In `CommandHost.InvokeAsync()` — currently a one-liner delegating to `RequiredResult.InvokeAsync()`
  — check `RequiredResult.Errors.Count > 0` first; if so, resolve an optional `IParseErrorHandler`
  from the service provider and return its result instead of delegating. When no handler is
  registered, fall through to the default behavior unchanged.
- **Outputs**: `JsonParseErrorHandler : IParseErrorHandler` renders a `CommandOutput` envelope to
  stderr via `Print(stderr: true)`, honoring `result.IsCompact()`.
- **Opt-in = registration**: one line in DI (`services.AddSingleton<IParseErrorHandler, JsonParseErrorHandler>()`),
  wrapped in an Outputs extension for ergonomics. Once present, every parse error renders as JSON
  with no per-call flag.

### Exit code

The parse-error path returns **1**, matching both `ParseErrorAction` and `GlobalErrorHandler`.
Avoid the reserved codes 255 (unhandled), 254 (cancelled) and 253 (option-handler error) — see
`project.md`.

## Implementation

### Files

- `Albatross.CommandLine/CommandHost.cs` — the `InvokeAsync()` seam.
- `Albatross.CommandLine/IParseErrorHandler.cs` — new; mirror `ICommandErrorHandler.cs`.
- `Albatross.CommandLine/ICommandErrorHandler.cs` — `ErrorSource` lives here and needs a new member
  (below).
- `Albatross.CommandLine.Outputs/JsonParseErrorHandler.cs` — new; mirror `GlobalErrorHandler.cs`.
- `Albatross.CommandLine.Outputs/Extensions.cs` — registration helper. Reuse `Serializer`, `Print`,
  `IsCompact`, `CommandOutput`.
- `docfx_project/articles/` — document the feature; pairs with the output-strategy article.

### The envelope shape has changed since this task was written

The original draft specified `Error = "ParseError"` and `ErrorDetail = <joined messages>`. Those
properties **no longer exist**. Per the unified error contract (2026-07-13, recorded in
`project.md`), `CommandOutput` now carries `IReadOnlyCollection<ErrorOutput>? Errors`, where
`ErrorOutput` is `{ Source, Symbol, Message, Detail }` — `Detail` running through
`JsonDetailConverter` so JSON detail text is inlined as a `JToken` rather than a quoted string.

So a parse error should produce **one `ErrorOutput` per `result.Errors[]` entry**, not a single
joined string. That also settles the original open question about joined-vs-structured: the
contract already decided it. Per entry:

- `Source` — needs a **new `ErrorSource.ParseError = 5` member**; the enum currently has no value
  that fits (`CommandHandler`, `OptionHandler`, `ServiceRegistration`, `CommandTaskCancellation`,
  `OptionTaskCancellation`). Add it with an XML doc comment matching the existing style.
- `Symbol` — best-effort from `ParseError.SymbolResult` where available; note `ErrorOutput`'s
  constructor deliberately drops `Symbol` when `Source == CommandHandler`, which does not apply
  here.
- `Message` — the `ParseError.Message`.
- Envelope `Command` — `result.CommandResult.Command.GetCommandKey()`; `ExitCode` — 1.

### DI timing

The scope exists after `Build()`, and a failed parse still yields a valid `RequiredResult` (just
one with `Errors`), so resolving an optional handler on the parse-error path is safe.

### Testing

Under `Albatross.CommandLine.Test/`, following the existing `ClassName_MethodName.cs` naming: parse
a known-bad command line with the handler registered and assert one JSON `CommandOutput` on stderr
carrying one `ErrorOutput` per parse error, exit code 1, and that valid command lines are
unaffected. Cover the not-registered case too — the default `ParseErrorAction` rendering must be
untouched.

### Open questions

- Should an appsettings.json toggle also be offered, or does opt-in stay purely DI-registration
  based? The latter matches "config-based opt-in" as originally stated.
- Check `Anchor.CommandLine` (`C:\app\anchor\Anchor.CommandLine`) for prior art on JSON parse-error
  rendering before implementing.
- The System.CommandLine mechanics above were verified in 2026-07-09 against a source checkout that
  is **no longer valid**: `C:\app\public\command-line-api` sits at `v2.0.0-beta3.22114.1` (2022,
  the old middleware API), and there is no `dependency.json` on this machine. Re-verify
  `ParseResult.Action` and `ParseErrorAction` against the package XML docs at
  `~/.nuget/packages/system.commandline/3.0.0-preview.5.26302.115/lib/net10.0/System.CommandLine.xml`,
  or fetch a v3 tag into that clone, before relying on them.

## Conclusion
