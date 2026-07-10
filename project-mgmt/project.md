# Albatross.CommandLine

status: active
created: 2026-07-08T12:35:28-04:00
updated: 2026-07-10T09:30:00-04:00
----

## Business Requirements

Albatross.CommandLine is a public, open-source .NET library that makes building
command-line applications on top of `System.CommandLine` faster and less error-prone.
It is distributed as a set of NuGet packages and consumed by developers building CLI
tools.

The problem it solves: `System.CommandLine` is powerful but low-level. Wiring up
commands, options, arguments, dependency injection, logging, configuration, and
validation by hand is verbose, repetitive, and easy to get wrong. Developers end up
writing large amounts of boilerplate to define each command and its parameters, and
`System.CommandLine` does not offer dependency injection or a structured execution
pipeline out of the box.

What success looks like:
- A developer defines a command by writing a plain parameters class decorated with
  attributes (`[Verb]`, `[Option]`, `[Argument]`) and a handler class — no manual
  command wiring.
- The library generates all the command plumbing at compile time, so there is no
  runtime reflection, and the result is AOT- and trimming-friendly.
- Dependency injection, logging, configuration, validation, error handling, and
  option preprocessing are available as first-class, opt-in features.
- The library stays broadly compatible with the .NET ecosystem so it can be adopted
  into existing projects without dependency conflicts.

Stakeholders: the library maintainer (RushuiGuan) and the .NET developer community
who consume the packages to build their own CLI tools.

## Technical Design

The library sits on top of `System.CommandLine` 2.0.2 and adds a code-generation layer,
a dependency-injection-aware execution pipeline, and optional integrations.

### Central abstraction: the Parameters class

Commands are defined declaratively. A **Parameters class** (conventionally named with a
`Params` suffix) is decorated with `[Verb<THandler>("name")]`. Its properties are
decorated with `[Option]` or `[Argument]` to describe the command's inputs. This single
class is the source of truth that ties together the command, its handler, and its
inputs. A matching **command handler** implements `IAsyncCommandHandler` (or derives from
`BaseHandler<TParams>`) and contains the actual logic.

### Compile-time code generation

`Albatross.CommandLine.CodeGen` is a Roslyn **incremental source generator** that runs
at compile time. It scans for `[Verb]`-annotated parameters classes and generates:

1. One partial `Command` class per verb (`{Name}Command.g.cs`), with strongly-typed
   `Option_*` / `Argument_*` properties and an empty partial `Initialize()` hook for
   customization.
2. `RegisterCommands()` — a DI registration extension that registers each parameters
   class (scoped) and its handler (keyed scoped), populating parameters instances from
   the parse result.
3. `AddCommands()` — builds the command hierarchy (including sub-command grouping from
   space-separated verb names like `"config set"`) just before parsing.

Because all wiring is statically generated, there is no runtime scanning, dynamic
discovery, or `Activator` usage — improving startup performance and enabling Native AOT
and trimming.

### Enhanced execution pipeline

`System.CommandLine`'s native pipeline runs 0+ option PreActions followed by a single
command Action, but lacks DI, dynamic short-circuiting, and cross-stage state sharing.
Albatross replaces these with its own internal actions:
- `AsyncOptionAction` — async, DI-enabled option handlers (PreActions).
- `GlobalCommandAction` — unified command execution with DI, global exception handling,
  and context management.

A single DI scope is created per command execution and shared across all option handlers
and the command handler. `CommandContext` (`ICommandContext`) is the per-execution
communication hub: it enables early short-circuiting, status tracking, sharing state
between option handlers and the command handler, and automatic disposal of any stored
values implementing `IDisposable`/`IAsyncDisposable`.

### Bootstrap model

Applications wire everything up through a fluent `CommandHost`:

```csharp
await using var host = new CommandHost("App Name")
    .RegisterServices(RegisterServices)
    .AddCommands()      // generated
    .Parse(args)
    .WithDefaults()     // optional: config + Serilog logging
    .Build();
return await host.InvokeAsync();
```

Ordering matters: config/logging extensions must run **after** `Parse()` because they
read parsed values such as `--verbosity`.

### Feature set built on this foundation

- **Reusable parameters** — custom `Option<T>`/`Argument<T>` subclasses (with
  `[DefaultNameAliases]`) applied via `[UseOption<T>]` / `[UseArgument<T>]` to centralize
  validation, descriptions, and defaults.
- **Option preprocessing & transformation** — `[OptionHandler<TOption, THandler>]` runs
  injectable async validation before the command; the 3-generic form
  `[OptionHandler<TOption, THandler, TOut>]` transforms an input value into a richer
  object passed to the handler.
- **Mutually exclusive parameter sets** — multiple parameters classes deriving from a
  shared base (`BaseParamsClass`) let one handler serve several related commands, with
  the generator producing a factory that picks the right derived type by command key.
- **Command customization** — partial `Initialize()` method on generated command classes
  for custom validators and configuration.
- **Logging & verbosity** — in v8, a recursive global `--verbosity`/`-v` option maps to
  `Microsoft.Extensions.Logging.LogLevel`, defaulting to `Error`, and is created
  automatically. **Changing in v9:** clean stdout/stderr is the default and the core library
  no longer creates `--verbosity`. A consumer that wants a recursive option adds it directly to
  the public `CommandBuilder.RootCommand` (reachable via `host.CommandBuilder.RootCommand`) before
  `Parse()` — no dedicated library API is needed. Commands own their output (see Key Design Decisions).
- **Custom global error handling** — `ICommandErrorHandler` maps unhandled exceptions to
  exit codes (reserved codes: 255 unhandled, 254 cancelled, 253 option-handler error).
- **Code analysis** — `Albatross.CommandLine.CodeAnalysis` Roslyn analyzer flags common
  attribute misuse (ACL0001–ACL0004) at compile time.

### Package structure

- **Albatross.CommandLine** — core library: `CommandHost`, attributes, interfaces,
  pipeline. Targets .NET Standard 2.1. In v8 it used conservative dependency versions
  (`System.CommandLine` 2.0.2, `Microsoft.Extensions.Hosting` 8.0.1) for maximum
  compatibility across .NET 6+. **In v9 (verified in code) it multi-targets `netstandard2.1;net10.0`,
  moves to `System.CommandLine` 3.0 prerelease, and raises `Microsoft.Extensions.Hosting` to 10.0.9**
  (the earlier 8.0.1 hold-back was dropped now that net8/9 are EOL/near-EOL; see Key Design Decisions).
- **Albatross.CommandLine.CodeGen** — the Roslyn incremental source generator. **Not published as a
  standalone package (v9):** it is bundled into the `Albatross.CommandLine` package's
  `analyzers/dotnet/cs` (see `CodeGen.Payload.targets`), so referencing the core package
  auto-activates it. Still built and consumed as a project reference in-repo (by the core lib and
  tests). See Key Design Decisions.
- **Albatross.CommandLine.CodeAnalysis** — development-only Roslyn analyzer, **published and opt-in**:
  consumers add it directly (see the code-analysis article); it is *not* bundled into core.
- **Albatross.CommandLine.Defaults** — optional Serilog logging + JSON configuration via
  `.WithDefaults()` / `.WithConfig()` / `.WithSerilog()`. Forward-looking dependency
  versions (Albatross.Config/Logging, Microsoft.Extensions.* 10.x). **v9 change:** logging
  defaults to a **file** sink (no console sink), leaving stdout/stderr for command output.
- **Albatross.CommandLine.Inputs** — reusable file/directory `Option`/`Argument` types
  with validation.
- **Albatross.CommandLine.Outputs** *(new in v9)* — the formalized output strategy, centered on
  the `CommandOutput` envelope: a shared JSON serializer plus the `Print`/`--query`(JmesPath)/
  `--compact` render surface. Carries the output-only dependencies (Spectre.Console.Json, a
  JmesPath library, Newtonsoft.Json) so the core stays dependency-light. Does **not** ship the
  Anchor-specific paging/domain inputs or a default error handler — those stay with the consuming
  app. Lifted from Anchor's `Anchor.CommandLine` (see Key Design Decisions).
- **Albatross.CommandLine.Test** — xUnit unit tests.
- **Sample.CommandLine** / **LoggingTest** — example applications.

### v9 implementation status (verified in code, 2026-07-08)

v9 is under active construction, not only designed. `Directory.Build.props` sets `Version` to
`9.0.0`. Reconciling the plan against the actual source:

- **Done in code:**
  - Core `Albatross.CommandLine` is migrated to `System.CommandLine 3.0.0-preview.5.26302.115`
    and multi-targets `netstandard2.1;net10.0` (its `Microsoft.Extensions.Hosting` reference is now
    `10.0.9`; see Key Design Decisions).
  - `Albatross.CommandLine.Outputs` exists and builds — `CommandOutput`/`CommandOutput<T>`, a
    **single** shared serializer (the Anchor double-definition did not carry over), the
    `Print`/`IsCompact`/`PrintSuccess`/`PrintError` surface, the `QueryOption`/`CompactOption`
    options, and a simplified opt-in `GlobalErrorHandler` (no new dependencies). Its README is
    written.
  - `Albatross.CommandLine.Defaults` now defaults to **file-based** Serilog logging (daily-rolling
    file under `IApplicationPath.LogRoot`, no console sink); the `Albatross.Logging` dependency was
    removed in favor of direct Serilog packages. Builds clean. Defaults pins Hosting/DI at 10.0.9,
    matching core's Hosting 10.0.9.
  - **Clean-output-by-default / `--verbosity` removal is done**: `VebosityOption.cs` is deleted and
    the verbosity-to-log-level wiring in `CommandHost` is commented out, so `CommandBuilder` no longer
    adds any recursive option and stdout/stderr are clean by default. No replacement library API was
    needed — a consumer adds recursive options directly to `CommandBuilder.RootCommand` (see below).
    `LoggingTest` (the old manual console-logging demo) was removed.

## Key Design Decisions

- **v9 is a breaking major release built on lessons from real-world use** (decided
  2026-07-08): After v8 stabilized and was used across several projects, accumulated
  learnings warrant breaking *behavior* changes. Rather than absorb them piecemeal, they
  are batched into a single major version bump (8.x → 9.0). The specific breaking changes
  are being enumerated — see Open Questions.
- **v9 migrates from System.CommandLine 2.0.2 to the v3 prerelease** (decided 2026-07-08):
  The major bump is treated as the opportunity to move to the System.CommandLine v3
  prerelease line. Adopting a prerelease dependency in a major release is an accepted
  trade-off here because the version boundary already signals breaking change to consumers.
  Impact on the core library's .NET Standard 2.1 target and conservative-dependency stance
  is an open item — see Open Questions. **Assessment update (2026-07-08):** the maintainer
  compared the v3 prerelease source against Albatross's usage and found **no breaking changes
  so far** — at the API level the migration currently looks like a reference bump. This must be
  re-verified as the prerelease evolves toward GA (the API can still shift before the stable
  release). The core's `netstandard2.1` target is unaffected: the maintainer confirmed
  (2026-07-08) that v3 still supports `netstandard2.1`, so the broad-compatibility stance holds.
  **Realized in code:** core `Albatross.CommandLine.csproj` now references
  `System.CommandLine 3.0.0-preview.5.26302.115` on a `netstandard2.1` target.
- **v9 default output is clean stdout/stderr; the recursive `--verbosity` option and
  automatic console logging are no longer created by default** (decided 2026-07-08): v8
  always added a recursive global `--verbosity`/`-v` option (defaulting to `Error`) to the
  root command, on the assumption that every CLI should emit some logging output. That
  assumption was rejected after real-world use: it is convenient in enterprise settings
  where the fastest way to build a CLI is to log to the console, but it is a poor *default*.
  A CLI whose job is to produce clean, parseable output should not carry a logging flag it
  did not ask for or emit log lines by default. v9 makes clean stdout/stderr the default and
  gives each command direct control over what it writes; logging (and the `--verbosity`
  option that drives it) becomes explicit opt-in. **Breaks:** any v8 consumer that relied on
  the automatically-present `--verbosity` option or on default console logging must now opt
  in explicitly. The opt-in mechanism is addressed by the next decision.
- **The core library removes `--verbosity`; consumers add their own recursive options via the
  existing `CommandBuilder.RootCommand` — no new library API** (decided 2026-07-08, refined
  2026-07-08): `Albatross.CommandLine` (core) drops the built-in `--verbosity` entirely. An earlier
  plan called for a dedicated "first-class facility" to let consumers register recursive options with
  minimal ceremony, but on inspection **no such facility is needed**: `CommandBuilder.RootCommand` is
  already public and reachable as `host.CommandBuilder.RootCommand`, so a consumer creates an
  `Option<T>` with `Recursive = true` and adds it to the root command before `Parse()` (the timing that
  makes the value available to post-parse steps such as logging setup). This keeps the "should every
  CLI log?" policy out of core while preserving the ability to add `--verbosity` — or any cross-cutting
  option — with the framework's existing extensibility rather than a bespoke API. Generalized
  principle: v9 never forces cross-cutting/global options on consumers — this holds not just for
  `--verbosity` but for the library's own `Albatross.CommandLine.Outputs` options (`--query`/`--compact`),
  which are likewise opt-in, not auto-recursive.
- **`Albatross.CommandLine.Defaults` defaults to file-based Serilog logging, keeping the
  console free for command output** (decided 2026-07-08): The complement of the
  clean-output-by-default decision. In v8, `WithSerilog()`/`WithDefaults()` configured
  console logging. In v9, the Defaults package configures Serilog to write to a **file** by
  default and attaches **no console sink**, so stdout/stderr belong entirely to the command's
  own output. Rationale: log lines and program output must not share a stream — mixing them
  corrupts the command's output contract (below) for anything parsing it. **Breaks:** v8
  consumers that relied on `WithSerilog()`/`WithDefaults()` emitting to the console; logs now go
  to a file and console logging becomes an explicit opt-in.
  **Implemented (2026-07-08, builds clean):** `WithSerilog()` configures Serilog via
  `IHostBuilder.UseSerilog((ctx, services, cfg) => …)`, writing a **daily-rolling file** under
  `IApplicationPath.LogRoot` (resolved from DI — the consumer must register an `IApplicationPath`;
  a missing registration throws a guiding exception) with `MinimumLevel.Information`,
  `Enrich.FromLogContext`, and no console sink. The file name is `{entryAssemblyName}-.log`.
  The **`Albatross.Logging` dependency was removed** in favor of a direct Serilog reference
  (Serilog 4.3.0, Serilog.Extensions.Hosting 8.0.0, Serilog.Sinks.File 6.0.0); the old
  `SetupSerilog`/verbosity-driven console path is gone. Follow-on note: the file log level no
  longer reads `--verbosity` (that option is being removed from core anyway).
  **Config-file tuning added (2026-07-08, builds clean):** the hard-coded `Information` minimum
  level made verbosity a recompile-only knob. `WithSerilog()` now layers
  `ReadFrom.Configuration(context.Configuration)` (package `Serilog.Settings.Configuration` 8.0.4)
  on top of the code defaults, so a `Serilog` section in `appsettings.json` (already loaded by
  `WithConfig()`) can override the level, add per-namespace `MinimumLevel:Override`s, and add extra
  sinks/enrichers — editable at deploy time without recompiling. Kept as a **hybrid**, not
  pure-config: the file sink and its `IApplicationPath.LogRoot` path stay in code (the path is
  environment-aware — user-mode vs service-mode — and would lose that if hard-coded in JSON), and
  `Information` remains the code default. Ordering is deliberate — `.MinimumLevel.Information()` is
  applied *before* `ReadFrom.Configuration`, so config overrides only the keys it specifies and an
  absent/empty `Serilog` section leaves behavior identical to before. Because `WithConfig()` also
  loads `appsettings.{DOTNET_ENVIRONMENT}.json`, the level can differ per environment for free.
  Chose an `appsettings.json` section over a dedicated `serilog.json` file: no extra config-builder
  plumbing and it inherits the existing environment layering (the maintainer was asked but away;
  proceeded on the recommended option, reversible to a dedicated file if preferred).
  **Config location — read from `ConfigRoot`, not the bin folder (resolved 2026-07-08 by maintainer):**
  `WithSerilog()` auto-resolves `IApplicationPath` from DI to place *logs* under `LogRoot`, but
  `WithConfig()` builds its `IConfiguration` eagerly (before the DI container exists), so it cannot
  resolve `IApplicationPath` the same way and defaulted its base path to `AppContext.BaseDirectory`
  (the bin folder). That is the wrong home for the tunable `Serilog` section: the bin folder is part
  of the deployment artifact (overwritten on redeploy) and often read-only. The maintainer resolved
  this by passing `appPath.ConfigRoot` through the existing `configDirectory` parameter —
  `.WithDefaults(appPath.ConfigRoot)` — so `appsettings.json` (and its `Serilog` section) is read from
  the stable, environment-aware, writable `ConfigRoot` that `IApplicationPath.Init()` creates. A
  dedicated `WithConfig(IApplicationPath)` overload (auto-layering bin defaults + `ConfigRoot`
  overrides and registering `appPath`) was considered and declined in favor of the simpler existing
  parameter; note the parameter *replaces* the base path rather than layering, so config lives solely
  in `ConfigRoot` in this pattern.
  **Finalized (2026-07-08, verified end-to-end):** a brief detour explored a *dedicated* `serilog.json`
  (the `Albatross.Hosting` convention) but it was rejected — separation is purely organizational, not
  functional (`ReadFrom.Configuration` reads a single merged `Serilog` section and cannot tell which
  file a key came from), and a separate file is a *net negative* for a CLI because it is one more
  artifact to place in the hard-to-reach, consuming-app-specific `ConfigRoot`. The tunable config
  therefore lives in the `Serilog` section of `appsettings.json` (loaded from `ConfigRoot`). Because no
  config file can be reliably shipped to `ConfigRoot`, the sensible baseline must live in **code**:
  `WithSerilog()` sets `MinimumLevel.Information()` + `Enrich.FromLogContext()` and always adds the
  file sink (path from `LogRoot`, name `{entryAssemblyName}-.log`, daily rolling, const
  `DefaultOutputTemplate`), then layers `ReadFrom.Configuration` on top. Verified by running the Sample
  (`test logging`, `test error-handling`): levels filter at `Information`, entries are line-separated,
  and exceptions render full stack traces to the file. Two field notes: (1) an **env-var-only sink
  fragment is invalid** — a secret supplied via `Serilog__WriteTo__<sink>__Args__…` needs the sink's
  `Name` element defined in a JSON file, else `ReadFrom.Configuration` throws at startup (fail-fast, by
  design); (2) `DefaultOutputTemplate` includes `{ThreadId}` but the code baseline does not call
  `.Enrich.WithThreadId()`, so it renders empty unless a consumer adds the enricher via config — wire
  `.Enrich.WithThreadId()` in code (the `Serilog.Enrichers.Thread` package is referenced) or drop the
  token. `Serilog.Settings.Configuration` 8.0.4 + `Serilog.Enrichers.Thread` 4.0.0 are the added
  dependencies; the inert `serilog.json` starter (and its `CopyToOutputDirectory` csproj entry) was
  removed.
- **v9 formalizes a first-class command output strategy: output is a consumed contract for
  humans *and* automation/AI, favoring a stable, correct schema over visual compactness**
  (decided 2026-07-08): Modeled on the Anchor CLI's output contract. Now that stdout/stderr
  is clean by default and logging is off the console, a command's stdout is purely its result
  — and that result is routinely piped to `jq`, queried with JmesPath, and fed to LLM/agent
  tooling as well as read by humans. The library standardizes an output surface commands write
  through (replacing ad-hoc `Console.WriteLine`), governed by these principles lifted from
  Anchor:
  - Output is a **contract**; compaction must be **lossless** — the moment it changes meaning
    it is the wrong lever.
  - **Drop nulls** (for a nullable field, present-and-null and absent are semantically
    identical), but **keep empty arrays/objects** (a positive "exists, zero items" statement;
    preserves the "collections are always arrays" invariant that scripted/JmesPath/AI consumers
    rely on) and **keep default values** (`false`/`0`/zero-enum members are printed, so nothing
    must be inferred from a missing key).
  - **Shape less-data per call at the query layer (JmesPath)** rather than mutating the
    canonical output for every consumer.
  - **Color is free and non-destructive** — rendered for a TTY, ANSI stripped for non-TTY/piped
    output, so it never harms the machine/AI path.
  This resolves the earlier open question about the command-owned output surface. **Resolved
  (2026-07-08):** the strategy ships as a new dedicated package, `Albatross.CommandLine.Outputs`
  (keeping the core dependency-light), and standardizes **JSON as the output format with
  built-in JmesPath querying** — Spectre.Console.Json for TTY coloring, a JmesPath library for
  per-call shaping. The concrete patterns are lifted from Anchor's reference implementation —
  see the next decision.
- **`Albatross.CommandLine.Outputs` centers on the `CommandOutput` envelope lifted from
  `Anchor.CommandLine`; the paging/query inputs and error handler are *not* lifted**
  (decided 2026-07-08): Anchor's `Anchor.CommandLine` project
  (`C:\app\anchor\Anchor.CommandLine`) is the reference, but most of it is Anchor-specific.
  Only the genuinely reusable output pieces are promoted:
  - **`CommandOutput` / `CommandOutput<T>` envelope (the core pattern)** — a standard result
    record (`Command`, `Message`, `Error`, `ErrorDetail`, `ExitCode`, `LogFolder`, and generic
    `Data` ordered last via `[JsonProperty(Order = 100)]`) so every command emits one
    predictable, machine-parseable shape for both success and failure. This is the primary
    artifact the package exists to provide.
  - **Shared JSON serializer** — camelCase names, enums as names (`StringEnumConverter`),
    `NullValueHandling.Ignore` to drop nulls while **keeping** defaults and empty collections —
    the exact lossless-compaction contract from the output-strategy decision above.
  - **The `Print` render surface** that realizes JSON-first-with-JmesPath: object → optional
    JmesPath transform → indented, Spectre-colored JSON on a TTY (ANSI stripped when piped),
    scalars raw for scriptability, a compact single-line mode, and stderr routing for errors,
    plus the `--query`/`-q` and `--compact` options that drive it. These are the output-strategy
    options; they are the exception to "the Inputs namespace stays in Anchor."
  - **`--query`/`--compact` are provided as opt-in reusable options, never auto-registered as
    recursive** (per maintainer, 2026-07-08) — the `--verbosity` lesson applied to the library's
    own options. The package ships the option *types*; the consumer decides whether and where to
    apply them (e.g. `[UseOption<QueryOption>]`, or on their own shared base-params class as
    Anchor does). The library sets no cross-cutting-option policy on the consumer's behalf.
  - **What is deliberately NOT lifted**: the rest of `Anchor.CommandLine.Inputs` is Anchor-
    specific and stays put — the paging/search inputs (`skip`/`take`/search/`show-secrets`) and
    all domain options (client/tenant/OIDC/entity, `DatabaseProvider`). They are the consuming
    application's concern, not the library's.
  - **`GlobalErrorHandler`: Outputs ships a simplified, dependency-light, opt-in handler — not
    auto-wired** (revised 2026-07-08, implemented + builds clean). Earlier plan was that no error
    handler ship in the library and each app wire its own; the concern was forcing dependencies
    and app-specific opinions. The landed design threads that needle: a generic
    `Albatross.CommandLine.Outputs.GlobalErrorHandler : ICommandErrorHandler` that logs the
    exception and prints a `CommandOutput` envelope (`Error` = exception type name, `ErrorDetail`
    = message) to stderr, returning exit code 1. It lives in **Outputs** (error reporting is
    output) and adds **no new dependencies** — only core (`ICommandContext`/`ICommandErrorHandler`),
    `CommandOutput`/`Print`, `ILogger`, and Newtonsoft (already present). It is **opt-in**: the
    consumer registers `ICommandErrorHandler` (same capability-not-policy stance as
    `--query`/`--compact`), or replaces it with their own. Deliberately dropped from Anchor's
    richer version: (a) **semantic-error unwrapping** (an `Albatross.Exceptions`/Input opinion —
    left to consumer subclasses); (b) **`LogFolder` in the envelope** — re-adding it needs
    `IApplicationPath` (→ `Albatross.Config` dependency in Outputs), so the "point the user to the
    file logs" affordance was traded away for dependency-lightness. A consumer who wants it
    subclasses the handler or adds an optional `IApplicationPath?` ctor param. This choice keeps
    **Defaults clean** — it does *not* depend on Outputs or `Albatross.Exceptions` (an earlier
    attempt to host the handler in Defaults added both and was reverted).
  Cleanup to do while lifting: Anchor currently defines the shared `Serializer` twice (in its
  `Inputs` and `Outputs` namespaces) — collapse to a single definition in the Outputs package.
  **Resolved in code:** the shipped `Albatross.CommandLine.Outputs` defines the serializer once
  (in its `Extensions`); the Anchor duplication did not carry over.
- **`Albatross.CommandLine.Outputs` and `Albatross.CommandLine.Defaults` stay separate sibling
  opt-in packages — not merged** (decided 2026-07-08): Both are opinionated (unlike the
  dependency-light core), and `CommandOutput.LogFolder` pairs naturally with file-based logging,
  so merging them was considered. Rejected because the coupling is **semantic, not technical**:
  `CommandOutput` references only Newtonsoft.Json; `LogFolder` is an optional `string?` whose
  value comes from the **config/path layer** (Anchor sources it from `IApplicationPath.LogRoot`),
  not from Serilog or Defaults, and it self-drops from output when null. The real shared
  dependency is therefore the *log-folder path*, which belongs in the config layer and is read by
  both — Defaults *writes* logs there, Output *reports* it; neither needs the other. The two also
  have disjoint, heavy dependency graphs (Defaults → Serilog / Albatross.Config / Albatross.Logging
  / Hosting; Output → Spectre.Console.Json / JmesPath / Newtonsoft.Json). Merging would force
  Serilog onto consumers who want structured JSON output but their own or no logging, and force
  the JSON-output contract onto consumers who only want file-logging defaults — two genuinely
  independent choices. Keeping them separate matches the v9 philosophy (orthogonal opinionated
  concerns → separate packages; capability over forced policy). They are documented as a
  **recommended pairing**, and the "one call sets up everything" ergonomics is handled with
  docs/a sample rather than a merge.
- **`QueryOption` and `CompactOption` stay in `Albatross.CommandLine.Outputs` — not moved to
  `Albatross.CommandLine.Inputs`** (decided 2026-07-08): Considered relocating the two options to
  the Inputs package (the general home for reusable `Option`/`Argument` types) and having Outputs
  reference Inputs. Rejected. They are output-strategy options with no meaning outside the render
  path — `--query` produces a `JmesPathExpression` consumed only by `Print`, and `--compact` is
  read only by `Print`/`IsCompact`. Decisively, `QueryOption` depends on `JmesPath.Net`; moving it
  into Inputs would push that dependency onto a package that is deliberately light (file/directory
  options only), so every consumer wanting just `InputFileOption` would inherit JmesPath. Keeping
  them in Outputs also avoids coupling two otherwise-independent sibling packages. Same
  don't-force-dependency-graphs principle as the Outputs/Defaults separation.
- **v9 ships as prerelease until System.CommandLine v3 reaches GA** (decided 2026-07-08):
  Because v9 depends on the System.CommandLine v3 prerelease, v9 itself will only be
  published as a prerelease (e.g. `9.0.0-preview.*`). A stable 9.0.0 cannot ship until the
  System.CommandLine v3 stable release, expected ~November 2026. v9 development, breaking
  changes, and feedback can therefore proceed and be released continuously on the
  prerelease channel without waiting; only the stable tag is gated.
- **Compile-time code generation over runtime reflection**: A Roslyn incremental source
  generator wires up commands, DI, and the command tree at compile time. Reflection-based
  discovery was rejected because it hurts startup performance and is incompatible with
  Native AOT and trimming. The trade-off is that all commands must be statically known at
  compile time.
- **Parameters class as the single source of truth**: Command, handler, options, and
  arguments are all declared from one attributed class rather than spread across manual
  builder calls. This minimizes boilerplate and keeps a command's definition in one place.
- **Nullability drives option/argument requiredness**: Requiredness and arity are inferred
  from property nullability and type rather than always requiring explicit configuration.
  The C# `required` keyword is deliberately **not** used because it is unavailable on all
  supported target frameworks; `[Option(Required = …)]` / `DefaultToInitializer` override
  when needed.
- **Core library multi-targets `netstandard2.1;net10.0`, tracking System.CommandLine's own
  TFMs** (decided 2026-07-09; supersedes the earlier "core must remain netstandard2.1-only"
  stance): The guiding rule is *don't be more restrictive than the dependency you wrap*. The v3
  prerelease Albatross references (`3.0.0-preview.5.26302.115`) ships `netstandard2.0` + `net10.0`
  (its csproj is `$(NetMinimum);netstandard2.0`; Arcade resolves `NetMinimum` to `net10.0` in the
  .NET 10 cycle). Albatross therefore keeps a **netstandard** floor and adds a **net10.0** leg that
  matches System.CommandLine's modern target. Two sub-decisions:
  - **The netstandard floor stays at 2.1, not 2.0.** Dropping to 2.0 (which would match
    System.CommandLine's reach exactly, adding .NET Framework 4.6.1+) was investigated in prior
    work and rejected as too hard to support. 2.1 is the deliberate floor.
  - **The modern leg is net10.0, and a library TFM is a *floor, not a runtime mandate*.** Earlier
    discussion weighed net8-only / net10-only / dropping netstandard; all moot once framed
    correctly: a `netstandard2.1;net10.0` build is consumed by net8/net9/net10+ apps alike via
    rollforward (NuGet picks the net10 asset for net10 apps, the netstandard2.1 asset for older),
    so keeping the netstandard leg maximizes reach at zero cost while the net10 leg gives modern
    consumers the better surface. net10 (current LTS) is chosen over net8 (EOL Nov 2026, ≈ v9's
    own GA timing) simply because it is what System.CommandLine ships.
  The net10 leg sets `IsAotCompatible=true` (valid on net7+ only, so gated by a TFM condition to
  keep the netstandard2.1 leg from emitting NETSDK1210) to self-validate the AOT goal and unlock
  `required`/newer BCL for net10 consumers. Modern conveniences (Serilog, config) still live in
  the optional Defaults package. `Microsoft.Extensions.Hosting` is referenced at 10.0.9 on both legs
  (see the dedicated Hosting decision below).
- **Core tracks current `Microsoft.Extensions.Hosting` 10.x (10.0.9), reversing the earlier
  hold-at-8.0.1 stance** (decided 2026-07-10; supersedes the 2026-07-08 decision to pin 8.0.1):
  Originally core pinned Hosting at 8.0.1 to avoid raising the transitive floor and forcing consumers
  onto Hosting 10.x for no benefit. That reasoning is now outweighed by lifecycle: .NET 8 reaches EOL
  Nov 2026 (≈ v9's own GA timing) and .NET 9 is already EOL, so the runtimes this library targets are
  moving to 10.x regardless. Core (and Defaults) now reference Hosting/DI **10.0.9**. **Accepted
  trade-off:** because core still ships the `netstandard2.1` leg, a net8/net9 consumer resolving that
  asset is pulled up to Hosting 10.x — the exact consequence the old decision avoided — but that is
  acceptable given those runtimes are EOL/near-EOL. Verified: core builds clean on both the
  `netstandard2.1` and `net10.0` legs with Hosting 10.0.9 (10.x still ships a netstandard-compatible
  asset). Defaults matches at 10.0.9.
- **Enhanced pipeline with a shared per-execution DI scope**: Replaces `System.CommandLine`'s
  native actions with `AsyncOptionAction` and `GlobalCommandAction` to add DI, async
  support, dynamic short-circuiting, and centralized error handling — capabilities the base
  pipeline cannot provide (its termination flag is read before execution, and it has no DI).
- **Input Transformation preferred over CommandContext for passing data**: Sharing data
  from option handlers to command handlers should use the type-safe `[OptionHandler<…,TOut>]`
  transformation pattern; storing values in `CommandContext` is supported but discouraged as
  a fallback.
- **Reserved exit codes for pipeline outcomes**: 255 (unhandled exception), 254 (cancelled),
  253 (option-handler error). Custom `ICommandErrorHandler` implementations must avoid these
  to prevent ambiguity.
- **Ship a companion Roslyn analyzer**: `Albatross.CommandLine.CodeAnalysis` surfaces
  attribute misuse (duplicate option names, bad `BaseParamsClass` inheritance, `OptionHandler`
  type mismatch, conflicting `[Option]`/`[Argument]`) as IDE diagnostics rather than as
  cryptic generated-code compiler errors or runtime failures.
- **The source generator is bundled into the core package, not published standalone** (decided
  2026-07-10): The `Albatross.CommandLine.CodeGen` DLL and its analyzer dependencies are packed into
  the `Albatross.CommandLine` package under `analyzers/dotnet/cs` (via the shared
  `CodeGen.Payload.targets`, using a `TargetsForTfmSpecificContentInPackage` target because
  `GeneratePathProperty` paths only resolve in the per-TFM inner build while `dotnet pack` collects
  static content at the outer evaluation). Referencing the core package therefore auto-activates the
  generator. `Albatross.CommandLine.CodeGen` is no longer published as a standalone NuGet package — it
  is simply omitted from the `.projects` file (the list that drives which projects CI packs/publishes),
  so no separate package is produced. Rationale: the generator emits code that depends on core's types, so it has no
  coherent standalone consumer; and publishing both would let a consumer reference core (bundled) *and*
  the standalone package, double-loading the generator (duplicate generated types). The project still
  exists and is consumed as a project reference in-repo (core and tests) via the `GetDependencyTargetPaths`
  mechanism, which is independent of packaging. `Albatross.CommandLine.CodeAnalysis` is the exception —
  it stays published and opt-in (consumers reference it directly), so it is not bundled. See the
  `bundle-codegen-into-core-package` task for the full implementation and verification.

## Open Questions

- **What are the specific v9 breaking behavior changes?** The "things learned" from using
  v8 across projects need to be enumerated so each can be recorded as its own design
  decision (what changes, what it breaks, why). Being collected from the maintainer.
  Recorded so far (see Key Design Decisions): clean-output-by-default; removal of built-in
  `--verbosity` (consumers add recursive options via `CommandBuilder.RootCommand`); file-based
  logging default in Defaults; formalized command output strategy.
- **Log file location — resolved (2026-07-08):** Defaults writes to
  `IApplicationPath.LogRoot` (from `Albatross.Config`), resolved from DI. The consumer registers
  the `IApplicationPath` (its `logRoot`/`userMode` config governs the actual directory), and the
  file name is `{entryAssemblyName}-.log` with daily rolling. This is the same
  `IApplicationPath.LogRoot` that Output's `CommandOutput.LogFolder` reports, so the path is a
  single contract point in the config layer as intended. Residual detail: whether Defaults should
  also `Init()` the path (create the directory) or rely on the consumer/Serilog to create it —
  currently the consumer calls `appPath.Init()` (Anchor pattern) and the File sink also creates
  the directory.
- **API assessment done (2026-07-08): System.CommandLine v3 introduces no breaking changes to
  Albatross's usage** per the maintainer's source-code comparison; the migration currently looks
  like a reference bump. **Correction (2026-07-09):** the v3 package Albatross references
  (`3.0.0-preview.5.26302.115`) ships `netstandard2.0` + `net10.0` — it does **not** target
  netstandard2.1. Core now multi-targets `netstandard2.1;net10.0` to track System.CommandLine's
  TFMs (see Key Design Decisions). Residual: re-verify the API before GA, since the prerelease
  API can still shift.
- **Core Hosting version — RESOLVED (2026-07-10): tracks 10.x (10.0.9).** The earlier
  hold-at-8.0.1 was reversed now that net8/9 are EOL/near-EOL; core and Defaults reference Hosting/DI
  10.0.9. Recorded in Key Design Decisions.
- **Clean-output / `--verbosity`-removal — DONE (verified in code 2026-07-08).** Core no longer
  registers a recursive `VerbosityOption`: `VebosityOption.cs` is deleted and the verbosity-to-log-level
  wiring in `CommandHost` is commented out, so stdout/stderr are clean by default. `LoggingTest` (which
  demonstrated the old manual console-logging path) has been removed. Verbosity is now controlled via the
  Serilog `MinimumLevel` configuration in `Albatross.CommandLine.Defaults` (file logging). No follow-up
  needed for recursive options: a consumer wanting a `--verbosity`-style flag adds an `Option<T>` with
  `Recursive = true` to `host.CommandBuilder.RootCommand` before `Parse()` — the existing public API,
  so the once-planned dedicated "facility" is unnecessary.
- The conventions doc notes a planned code-analysis warning for duplicate names arising
  from case-only property differences — ACL0001 covers *that* case, but a gap was found
  (2026-07-09): ACL0001 only inspects `[Option]` properties and groups by **property name**,
  so it misses duplicate *effective* option names from `[UseOption<T>]` reuse (a shared
  `[DefaultNameAliases]`) and from explicit aliases. These compile clean but throw at runtime
  ("more than one child named ..."). Tracked in `detect-duplicate-option-names.tsk.md`. Also
  confirm whether any other planned analyzers remain unimplemented.
- **Should Defaults adopt a `serilog.json` convention? — RESOLVED (2026-07-08): no.** After
  weighing the `Albatross.Hosting` dedicated-`serilog.json` convention, Defaults stays with the
  `Serilog` section in `appsettings.json`. Rationale and the code-owned baseline that this required
  are recorded in Key Design Decisions (the file-based-logging decision, "Finalized" note).

## Dependencies & Constraints

- **v9 stable release is gated on an external dependency**: v9 references the
  System.CommandLine v3 prerelease and can only ship a stable 9.0.0 after System.CommandLine
  v3 reaches GA (expected ~November 2026, not controlled by this project). Until then, v9
  is published only on the prerelease channel. v8.x remains the current stable line for
  consumers who cannot take a prerelease dependency.
- Built on `System.CommandLine` 2.0.2 (stable) through v8. The library predates this stable release
  and a migration guide from `2.0.0-beta4` is maintained; consumers on beta4 APIs must
  migrate (`AddValidator` → `Validators.Add`, `GetValueForOption` → `GetValue`,
  `SetHandler` → `SetAction`, `InvocationContext` removed, etc.).
- Core library multi-targets **`netstandard2.1;net10.0`**, tracking System.CommandLine's own
  TFMs (netstandard floor + its `NetMinimum` modern leg). Do not be more restrictive than the
  wrapped dependency, do not raise the netstandard floor to 2.0 (investigated and rejected as too
  hard to support), and do not drop the netstandard leg (a library TFM is a floor, not a runtime
  mandate — the netstandard2.1 asset serves net8/net9 consumers, the net10 asset serves net10+).
  Modern/heavier dependencies still belong in the Defaults package. Raised core dependencies are
  `System.CommandLine` (3.0 prerelease) and `Microsoft.Extensions.Hosting` 10.0.9 (see Key Design
  Decisions).
- Reusable `Option<T>` subclasses **must** expose a `(string name, params string[] aliases)`
  constructor; reusable `Argument<T>` subclasses **must** expose a `(string name)`
  constructor — the source generator relies on these signatures to instantiate them.
- All commands must be statically declared via attributes at compile time (a consequence
  of choosing source generation over reflection).
- `WithConfig()` / `WithSerilog()` / `WithDefaults()` must be called **after** `Parse()`.
- Option/argument names are derived by kebab-casing property names; property names that
  collide case-insensitively break generation (flagged by ACL0001).
