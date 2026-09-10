---
name: Global --json-help option emitting the full command subtree as JSON
status: started
created: 2026-08-25T22:44:29-04:00
priority: normal
tags: outputs json help discovery system-commandline v9
---

## Objective

Give a CLI an opt-in, recursive `--json-help` option that emits **the invoked command and its
entire subcommand subtree** as one JSON document. Invoked on the root command it describes the
whole CLI in a single call — the surface an AI agent or a script needs to discover what a tool can
do without scraping `--help` text one command at a time.

Help is the last unstructured-text output the library produces: successful results and runtime
exceptions already flow through the Outputs contract (`Print`, `GlobalErrorHandler`), and parse
errors are covered separately (`json-parse-error-and-help-output.tsk.md`). This task closes the
help half.

## Design

- **Lives in `Albatross.CommandLine.Outputs`.** That package owns the shared `Serializer` and the
  `Print` render surface, which this feature reuses wholesale. Core stays JSON-agnostic — no core
  change is needed for this task at all.
- **Opt-in, never auto-registered.** Follows the standing v9 principle in `project.md` → Key
  Design Decisions: capability over forced policy, never force a cross-cutting/global option on
  consumers (the `--verbosity` removal and the `--query`/`--compact` precedents). The package
  ships the option *type* plus a `CommandHost.UseJsonHelp()` ergonomic helper that adds it with
  `Recursive = true` to `CommandBuilder.RootCommand`; the CLI author decides whether to call it,
  or adds the option to a single command instead.
- **`JsonHelpOption : Option<bool>`**, not a `HelpOption` subclass. `Option.Argument` is
  `internal abstract` in System.CommandLine, so a bare flag cannot be subclassed from outside the
  framework — the flag must be built from `Option<bool>` with `Arity = ArgumentArity.Zero`.
  Constructor signature must be `(string name, params string[] aliases)`: the source generator
  relies on it for reusable option types (see `project.md` → Dependencies & Constraints), and
  `HelpOption` itself follows the same shape. Default name/aliases via
  `[DefaultNameAliases("--json-help", "--jh")]`, matching `CompactOption`/`QueryOption`.
- **A terminating `SynchronousCommandLineAction` with `ClearsParseErrors => true`**, mirroring
  `HelpAction`. Without `ClearsParseErrors` the framework's `ParseErrorAction` wins and
  `--json-help` would go unanswered on an otherwise-invalid command line — which is precisely
  when a consumer most wants to ask what the command accepts.
- **Output is a bare help document, not the `CommandOutput` envelope.** Follows
  [the envelope wraps outcomes, not data](project.md#key-design-decisions): `--json-help` has no
  side effect and reports no error — its sole job is to return the CLI's shape, so it emits pure
  JSON that a consumer can query directly without unwrapping a `data` key first.
- **Recursion is the whole point: the document covers the invoked command and every descendant.**
  `--json-help` on the root yields the entire CLI; on a subcommand it yields that subtree. No
  depth limit for now (see Open questions).
- **Inherited `Option.Recursive` options are listed once, on the document's top node** — the
  command `--json-help` was actually invoked on — each marked `recursive: true`. Descendant nodes
  list only their own options.

  Note this is about System.CommandLine's `Option.Recursive` ("declared here, also applies to
  every descendant command"), which is a different axis from the command tree the document walks.
  An option declared on the root with `Recursive = true` is valid on every leaf but appears in
  `Command.Options` only on the root, so a subtree document rooted at a leaf would otherwise omit
  it entirely and read as "this command does not accept `--compact`". Listing inherited options on
  the entry point makes every document self-contained wherever it was requested, without repeating
  the same option on every node of a large tree. The exception is harmless: at the root — the
  common case — the entry point *is* the declaring command, so nothing is duplicated at all.
- **Hidden symbols are included**, unlike default help — each carrying `hidden: true`. `Hidden`
  exists to keep a symbol out of a human's way; a machine consumer asking for the CLI's shape
  wants the complete picture, and the flag lets it decide for itself what to surface. The action
  returns exit code 0.

## Implementation

### Files

New, in `Albatross.CommandLine.Outputs/`:
- `HelpDto.cs` — **done (2026-08-25)**. Holds `HelpDto` (a command node, recursive through
  `Subcommands`), `OptionDto`, `ArgumentDto` and `ArityDto`, as `record class` with `init`
  properties matching `CommandOutput`'s style. Collections default to `[]` and the flags are
  non-nullable `bool`, so they survive the serializer's `NullValueHandling.Ignore` and always
  appear — the "collections are always arrays, defaults are kept" contract from the output-strategy
  decision. `HasDefaultValue` sits alongside `DefaultValue` because `NullValueHandling.Ignore`
  otherwise makes "no default" and "the default is null" indistinguishable. `ArityDto.Maximum`
  reports `int.MaxValue` for unbounded, as System.CommandLine does.
- `JsonHelpOption.cs` — **the option shape and `GetHelp` are done (2026-08-25); the terminating
  action is not.** `GetHelp(Command)` builds the whole document; `GetInheritedRecursiveOptions`,
  `CreateOption`, `CreateArgument` and `GetHelp` are `virtual` so a consumer can extend the model
  the way `GlobalErrorHandler.Convert` allows. The option is `Option<bool>` with
  `Arity = ArgumentArity.Zero`, verified to parse as a bare flag (`GetValue` returns true when
  present, no parse error).
- `UseJsonHelp` extension in the existing `Extensions.cs`. **To do.**

Prior art to mirror: `CompactOption.cs` (the reusable-option shape and `[DefaultNameAliases]`),
`GlobalErrorHandler.cs` (an Outputs component that renders through `Print`).

Also touched:
- `Sample.CommandLine/Program.cs` — wire `host.UseJsonHelp()` to demonstrate it.
- `docfx_project/articles/` — new article plus a `toc.yml` entry; cross-link from
  `whats-new-v9.md`.

### Model sources

All verified public in `System.CommandLine 3.0.0-preview.5.26302.115`:

- Command → `Name`, `Aliases`, `Description`, `Hidden`, `Subcommands`, `Options`, `Arguments`;
  full command path via core's `Extensions.GetCommandKey()`.
- Option → `Name`, `Aliases`, `Description`, `Hidden`, `Required`, `HasDefaultValue` /
  `GetDefaultValue()`, `Arity`, `HelpName`, `ValueType`, `Recursive`.
- Argument → `Name`, `Description`, `Hidden`, `Arity`, `HelpName`, `HasDefaultValue` /
  `GetDefaultValue()`, `ValueType`.

`Hidden` is on `Symbol`, so it is available uniformly on all three.

These map onto `HelpDto` / `OptionDto` / `ArgumentDto` one-for-one, except three fields the builder
has to compute rather than copy: `HelpDto.Path` (from `GetCommandKey()`), `ValueType` (a friendly
name, not a serialized `System.Type`) and `AllowedValues`.

`ValueType` is a `System.Type` and must be rendered as a friendly name rather than serialized —
unwrap `Nullable<T>`, handle arrays and collections. For an enum-valued option, emit the allowed
values (`Enum.GetNames`); that is high-value information for an AI consumer and is otherwise
invisible. `AcceptOnlyFromAmong` values are **not** publicly exposed — best-effort discovery is
`Option.GetCompletions(CompletionContext.Empty)`, to be verified.

Two things found by running the built model against a real command tree (2026-08-25), both since
handled:

- **A flag's `ValueType` is `void`**, not `bool` — `--help` and `--version` both render as `Void`
  if copied straight through. `GetTypeName` returns null for `void` so the key drops; the zero
  arity already tells a consumer the option takes no value.
- **An unbounded arity reports `100000`, not `int.MaxValue`.** That is System.CommandLine's own
  `MaximumArity` sentinel (confirmed: `ArgumentArity.ZeroOrMore.MaximumNumberOfValues == 100000`).
  It is reported verbatim rather than normalized, and `ArityDto.Maximum` documents it.

### Gotchas found while scoping

1. **`IsCompact()` is blind to recursive options.** `Command.Options` explicitly *excludes*
   options inherited from a parent where `Recursive = true` — documented in the v3 XML docs:
   "This collection doesn't include options on parent commands where Option.Recursive is true.
   Those options are valid under the current command but don't appear in this collection."
   `Extensions.IsCompact()` reads exactly that collection
   (`result.CommandResult.Command.Options.OfType<CompactOption>()`), so a consumer who adds
   `CompactOption` recursively at the root gets `false` on every subcommand. Fix by walking
   ancestors, or by resolving through `result.GetResult(option)`. Decide whether to fix it in this
   task or split it out — this task depends on `IsCompact` being correct to honor `--compact`.
2. **`--query` does not arrive through the normal path.** The `JmesPathExpression` is produced by
   `ParseQueryExpression`, an option *pre-action* handler. Pre-action handlers are skipped whenever
   a terminating option is present — `CommandContext.HasShortCircuitOptions` returns true precisely
   because it tests `x.Option.Action?.Terminating == true`, which this action is. If `--query`
   support is wanted on `--json-help`, the action must locate the `QueryOption`, read its raw
   string, and parse it itself (`new JmesPath().Parse(text)`); Outputs already depends on
   JmesPath.Net. Guard against an invalid expression and fail with exit code 1.
3. **The System.CommandLine clone on this machine is stale and must not be used as reference.**
   `C:\app\public\command-line-api` is checked out at `v2.0.0-beta3.22114.1` (2022) — the old
   middleware/`InvocationContext` API, with no `HelpAction` or `ParseErrorAction`. It bears no
   resemblance to the v3 preview this project builds against, and no `dependency.json` exists on
   this machine to point elsewhere. Verify the API against the package XML docs at
   `~/.nuget/packages/system.commandline/3.0.0-preview.5.26302.115/lib/net10.0/System.CommandLine.xml`,
   or fetch a v3 tag into that clone first.

### What comes for free

- `CommandContext.HasShortCircuitOptions` already suppresses every other option handler when a
  terminating option is present, so `--json-help` short-circuits the pre-action pipeline with no
  new code.
- The action needs no DI — it builds the model straight from the `ParseResult` — so it works
  regardless of how the host is configured.
- `CommandHost.InvokeAsync()` delegates to `RequiredResult.InvokeAsync()`, which dispatches to the
  terminating option action. No `CommandHost` change is required.

### Ordering constraint

`UseJsonHelp()` must be called **before** `.Parse(args)`: `CommandBuilder.BuildTree()` runs inside
`Parse()`, and the option has to be on the root command by then. This is the same rule documented
for consumer-added recursive options in
`docfx_project/articles/logging-verbosity.md` § "Adding Your Own Recursive Option". The helper
should return `CommandHost` so it chains ahead of `.Parse(args)`.

### Testing

Under `Albatross.CommandLine.Test/`, following the existing `ClassName_MethodName.cs` naming
(e.g. `CommandBuilder_Add.cs`). Build a small command tree with nested subcommands, options with
defaults/required/enum values, an argument, a hidden command, and a recursive option at the root,
then assert:

- the full subtree is emitted, not just one level;
- hidden symbols are present and carry `hidden: true`;
- a root-declared recursive option appears on the document's top node marked `recursive: true`
  when `--json-help` is invoked on a nested command, and is not repeated on descendants;
- defaults, `required`, arity and enum allowed-values are correct;
- exit code is 0;
- `--json-help` still renders when the rest of the command line is invalid (the
  `ClearsParseErrors` behavior);
- `--compact` produces a single line.

### Open questions

- Fix the `IsCompact()` recursive-option blind spot here, or split it into its own task? It is a
  pre-existing bug that this feature would otherwise inherit.
- Is allowed-value discovery beyond enums (`AcceptOnlyFromAmong`) reachable via
  `Option.GetCompletions(CompletionContext.Empty)`, or should it be dropped?
- Should `--json-help` support `--query`? Doable per gotcha 2, but it means the action parses the
  JmesPath itself. A consumer can equally pipe the output to `jq`.
- Does a very large CLI need a depth limit, or is the full tree always acceptable?

## Conclusion
