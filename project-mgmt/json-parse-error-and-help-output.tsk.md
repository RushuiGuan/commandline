# Render parse errors and --help as JSON through the CommandOutput contract

status: new
created: 2026-07-09T16:54:19-04:00
priority: normal
tags: outputs json help parse-error system-commandline v9
----

## Objective

Let a CLI emit **parse errors** and **help** as JSON through the existing
`Albatross.CommandLine.Outputs` contract (`CommandOutput` / `Print` / shared `Serializer`),
instead of `System.CommandLine`'s default rendering (red error text + plain-text help to
stderr/stdout). This closes the last gap where a machine/AI/`jq` consumer still receives
unstructured text: successful results and *runtime* exceptions already go through the JSON
envelope (`Print`, `GlobalErrorHandler`), but **parse-time failures and help do not**.

Two independent, opt-in features with **different opt-in models** (per maintainer, 2026-07-09):

1. **Help — a runtime CLI flag.** A terminating `--json-help` / `--jh` option the *end user*
   passes. Opt-in by the CLI author either per-command or recursively across the whole CLI.
2. **Parse errors — a registration-time config.** The *CLI author* opts in once at bootstrap;
   thereafter the CLI **always** prints parse errors as JSON. Not a per-invocation flag.

## Reasoning

### Where this fits in v9

The v9 output strategy (see `project.md` → Key Design Decisions, "formalizes a first-class
command output strategy") makes stdout/stderr a clean, JSON-first contract. `CommandOutput`/
`CommandOutput<T>`, the shared `Serializer` (camelCase, enums-as-names, drop-nulls/keep-
defaults-and-empties), and the `Print` render surface (`--compact`, JmesPath `--query`, ANSI
stripped when piped) all live in `Albatross.CommandLine.Outputs`. `GlobalErrorHandler`
(Outputs) already renders *runtime* exceptions as a `CommandOutput` envelope on stderr.

Both features therefore belong in **Outputs** (it owns the serializer, the envelope, and
`Print`), and both must be **opt-in** — consistent with the standing v9 principle "capability
over forced policy; never force cross-cutting/global options on consumers" (the `--verbosity`
and `--query`/`--compact` precedents). The core library stays JSON-agnostic.

### System.CommandLine v3 mechanics (verified against C:\app\command-line-api, 2026-07-09)

- **`ParseErrorAction`** (`Invocation/ParseErrorAction.cs`) is `sealed`. When parsing produces
  errors, the parser sets the result's action to a `ParseErrorAction`, which writes each
  `parseResult.Errors[].Message` to `InvocationConfiguration.Error` (red), then invokes the
  proximate `HelpOption`'s action, and returns 1.
- **`HelpAction`** (`Help/HelpAction.cs`) is `sealed`; writes formatted text to
  `InvocationConfiguration.Output`, returns 0, and `ClearsParseErrors => true`.
- **`ParseResult.Action` is read-only** (`ParseResult.cs`: `public CommandLineAction? Action =>
  _action ?? CommandResult.Command.Action;`, backing field `private readonly`). So you
  **cannot** substitute a custom action into the parse result after `Parse()`. This is the key
  constraint: the parse-error path cannot be handled by "replacing the action" — it must be
  intercepted *before* invocation.
- **`Option.Action` is settable** (`Option.cs`: `public virtual CommandLineAction? Action {
  get; set; }`), and an option that carries an `Action` becomes the **terminating** action when
  present — this is exactly how `HelpOption` works (`HelpOption.cs`).
- **The non-generic `Option` cannot be subclassed externally**: `Option.Argument` is
  `internal abstract`, so a bare flag like `HelpOption` (which does `internal override Argument
  Argument => Argument.None`) can only be written inside System.CommandLine. External code must
  build the flag from **`Option<bool>`** with `Arity = ArgumentArity.Zero`.
- Library constraint (project.md → Dependencies & Constraints): reusable `Option<T>` subclasses
  **must** expose a `(string name, params string[] aliases)` constructor — the source generator
  relies on it. `HelpOption` already follows this exact signature; mirror it.

### Feature 1 — `--json-help` / `--jh` (runtime flag, per-command or recursive)

Model it on `HelpOption`, but as an externally-constructible `Option<bool>`:

- `JsonHelpOption : Option<bool>` in Outputs.
  - ctor `(string name, params string[] aliases)` → `base(name, aliases)`; default instance
    named `--json-help` with alias `--jh`.
  - `Arity = ArgumentArity.Zero` (flag; no value token).
  - `Recursive` left at the consumer's discretion — see opt-in below.
  - `Action` = a custom `SynchronousCommandLineAction` (below).
- The action:
  - Builds a **help POCO** from `parseResult.CommandResult.Command`: command name / full path
    (walk `CommandResult.Parent`), description, usage, and lists of options (name, aliases,
    description, required, default value, arity), arguments (name, description, arity), and
    subcommands (name, description). Skip `Hidden` symbols to match default help.
  - Serializes it with the Outputs `Serializer` and renders via `Print` (honor `--compact` if
    the command declares it: `parseResult.IsCompact()`). Returns 0.
  - Overrides **`ClearsParseErrors => true`** (like `HelpAction`) so `--json-help` works even
    when the rest of the command line has parse errors — otherwise `ParseErrorAction` would win.
- **Opt-in scope** (author's choice, both already supported by the framework):
  - *Per command*: add the option to a single command (the option's `Recursive = false`).
  - *Whole CLI*: `host.CommandBuilder.RootCommand.Options.Add(new JsonHelpOption { Recursive =
    true })` before `Parse()` — the same pattern the docs prescribe for a consumer-added
    `--verbosity`.
  - Provide a small ergonomic helper in Outputs (e.g. `CommandHost UseJsonHelp(name?, aliases?)`
    that adds a recursive `JsonHelpOption` to the root command) so the common case is one call.
    Consider `[UseOption<JsonHelpOption>]` viability too, but the direct-add pattern (as with
    `HelpOption`) is the natural fit for a terminating flag that binds to no handler parameter.

### Feature 2 — JSON parse errors (registration-time, always-on once configured)

Because `ParseResult.Action` is read-only, the parse-error path is intercepted **before**
`System.CommandLine` invocation. Introduce a seam in **core** symmetric with the existing
`ICommandErrorHandler` (which handles runtime exceptions):

- **Core** (`Albatross.CommandLine`): add `IParseErrorHandler` with `int Handle(ParseResult
  result)`. In `CommandHost.InvokeAsync()` (`CommandHost.cs:135`, currently `=> this.
  RequiredResult.InvokeAsync();`), check `RequiredResult.Errors.Count > 0` first; if so resolve
  an optional `IParseErrorHandler` from the service provider and, when present, return its
  result instead of delegating to `RequiredResult.InvokeAsync()`. When absent, fall through to
  the default (unchanged) behavior. This is the only core change; core stays JSON-agnostic (it
  only defines the seam, exactly as it does for `ICommandErrorHandler`).
  - Note: the DI scope exists after `Build()`; resolving an optional singleton/scoped handler
    is fine even for a failed parse (there is a valid `RequiredResult`, just with `Errors`).
- **Outputs**: `JsonParseErrorHandler : IParseErrorHandler` that emits a `CommandOutput`
  envelope to stderr via `Print(stderr: true)` — `Command` = best-effort command key from
  `result.CommandResult.Command.GetCommandKey()`, `Error` = "ParseError", `ErrorDetail` = the
  joined `result.Errors[].Message` (or a structured list — see open question), `ExitCode` = 1.
  Honor `result.IsCompact()`.
- **Opt-in = registration**: a one-liner registers the handler in DI, e.g.
  `services.AddSingleton<IParseErrorHandler, JsonParseErrorHandler>()` wrapped in an Outputs
  extension. "Config-based, always on once configured" (maintainer's phrasing) = once that
  registration is present, every parse error renders as JSON with no per-call flag.

### Exit codes

Parse-error path returns **1** (matches `ParseErrorAction` and `GlobalErrorHandler`); avoid the
reserved codes 255 (unhandled) / 254 (cancelled) / 253 (option-handler error) — see project.md.

### Files

- `Albatross.CommandLine/CommandHost.cs` — `InvokeAsync()` seam for `IParseErrorHandler`.
- `Albatross.CommandLine/` — new `IParseErrorHandler.cs` (mirror `ICommandErrorHandler.cs`).
- `Albatross.CommandLine.Outputs/` — new `JsonHelpOption.cs` (+ its action), `JsonParseErrorHandler.cs`,
  and registration/ergonomic extensions in `Extensions.cs`. Reuse `Serializer`, `Print`,
  `IsCompact`, `CommandOutput`. Prior art to mirror: `GlobalErrorHandler.cs`.
- Reference for the framework mechanics: `C:\app\command-line-api\src\System.CommandLine\{Option.cs,
  ParseResult.cs, Help/HelpOption.cs, Help/HelpAction.cs, Invocation/ParseErrorAction.cs}`.
- Docs: `docfx_project/articles/` — document both features (pairs with the output-strategy article).

### Testing

- Under `Albatross.CommandLine.Test/CodeGen/` (or a new Outputs test area): parse a known-bad
  command line with the handler registered → assert one JSON `CommandOutput` on stderr, exit 1,
  and that valid command lines are unaffected. For help: parse `--json-help` on a command with
  options/arguments/subcommands → assert the JSON help model matches (names, required, defaults,
  hidden excluded), that it returns 0, and that `--json-help` still renders when combined with
  otherwise-invalid input (the `ClearsParseErrors` behavior).

### Open questions (resolve during implementation)

- `ErrorDetail` for parse errors: a single joined string vs. a structured `string[]` / list of
  `{message, ...}` under `CommandOutput<T>.Data`. A list is more machine-friendly; confirm the
  shape against how consumers/AI expect to read it.
- Whether the `--json-help` action should honor `--query` (JmesPath) in addition to `--compact`.
- Whether to also offer an appsettings.json toggle for the parse-error handler, or keep it purely
  DI-registration based (the latter matches "config-based opt-in" as stated).
- Check `Anchor.CommandLine` (`C:\app\anchor\Anchor.CommandLine`) for any prior art on JSON help /
  parse-error rendering before implementing.
