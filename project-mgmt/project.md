---
name: Albatross.CommandLine
status: active
created: 2026-07-08T12:35:28-04:00
updated: 2026-09-14T05:18:32-04:00
---

## Business Requirements

Albatross.CommandLine is a public, open-source .NET library that makes building
command-line applications on top of `System.CommandLine` faster and less error-prone. It
is distributed as a set of NuGet packages and consumed by developers building CLI tools.

The problem it solves: `System.CommandLine` is powerful but low-level. Wiring up commands,
options, arguments, dependency injection, logging, configuration, and validation by hand is
verbose, repetitive, and easy to get wrong. Developers write large amounts of boilerplate to
define each command and its parameters, and `System.CommandLine` offers neither dependency
injection nor a structured execution pipeline out of the box.

What success looks like:

- A developer defines a command by writing a plain parameters class decorated with
  attributes, and the library generates the wiring.
- Common concerns — dependency injection, configuration, logging, validation, error
  handling, and output formatting — are available without bespoke plumbing.
- Mistakes surface at compile time as diagnostics rather than at runtime as failures.
- The generated applications start fast and stay compatible with Native AOT and trimming.
- Consumers take only the opinions they want; the core stays dependency-light and the
  opinionated pieces ship as separate optional packages.

The audience is .NET developers building internal tooling and shipped CLIs, including
enterprise teams whose command output is consumed by scripts and automation as well as read
by people.

## Technical Design

The library sits on top of `System.CommandLine` and adds a code-generation layer, a
dependency-injection-aware execution pipeline, and optional integrations. It ships as
several packages so consumers take only the opinions they want.

**The parameters class is the central abstraction.** A command is defined declaratively by a
plain class decorated with a verb attribute that names the command and points at its handler;
the class's properties are decorated to describe the command's options and arguments. A
matching handler class holds the logic. That one class is the source of truth tying together
the command, its handler, and its inputs.

**A source generator turns those classes into wiring at compile time.** It scans for
verb-annotated parameters classes and emits a strongly-typed command class per verb (with a
partial hook for customization), a dependency-injection registration extension that registers
each parameters class and handler and populates parameters from the parse result, and a
builder that assembles the command hierarchy — including sub-command grouping from
space-separated verb names — just before parsing. Because all wiring is statically generated
there is no runtime scanning or dynamic activation, which improves startup time and keeps
Native AOT and trimming viable.

**The execution pipeline replaces the host library's actions.** The native pipeline runs
option pre-actions followed by a single command action, but offers no dependency injection,
no dynamic short-circuiting, and no way to share state across stages. The library substitutes
its own async, DI-enabled option action and a unified command action that owns global
exception handling and context management. One DI scope is created per command execution and
shared across every option handler and the command handler. A per-execution command context
is the communication hub: it carries short-circuiting, status, shared state, and automatic
disposal of stored disposables.

**Applications wire everything through a fluent host.** The host registers services, adds the
generated commands, parses the arguments, optionally layers configuration and logging, and
builds. Ordering is part of the contract — the configuration and logging extensions read
parsed values, so they must run after parsing.

**Features built on this foundation:**

- Reusable option and argument types applied by attribute, centralizing validation,
  descriptions, and defaults across commands.
- Option preprocessing and transformation: an injectable async handler runs before the
  command to validate an option, or to transform its raw value into a richer object handed
  to the command handler.
- Mutually exclusive parameter sets — several parameters classes deriving from a shared base
  let one handler serve related commands, with the generator producing a factory that selects
  the right type by command key.
- Command customization through a partial initialization hook on each generated command class.
- A standard output surface and envelope that commands write through, with terminal-aware
  rendering and query-based shaping.
- A global error-handling contract that receives every error condition from an execution and
  decides how the user is notified and what exit code results.
- Compile-time diagnostics for attribute misuse from a companion analyzer.

**Package structure:**

- **Albatross.CommandLine** — the core: host, attributes, interfaces, and pipeline.
  Dependency-light and broadly targeted; the source generator is bundled inside it.
- **Albatross.CommandLine.CodeGen** — the Roslyn incremental source generator. Built in-repo
  and shipped inside the core package rather than published on its own.
- **Albatross.CommandLine.CodeAnalysis** — a development-only Roslyn analyzer, published
  separately and referenced opt-in.
- **Albatross.CommandLine.Defaults** — optional JSON configuration and file-based Serilog
  logging behind a single fluent call.
- **Albatross.CommandLine.Inputs** — reusable file and directory option and argument types
  with validation.
- **Albatross.CommandLine.Outputs** — the output contract: the result envelope, the shared
  serializer, the render surface with its query and compact options, and an opt-in global
  error handler. Carries the output-only dependencies so the core stays light.

Unit-test and sample projects accompany the packages but are not published.

## Key Design Decisions

### Compile-time code generation over runtime reflection

A Roslyn incremental source generator wires up commands, dependency injection, and the
command tree at compile time. Reflection-based discovery was rejected because it hurts
startup performance and is incompatible with Native AOT and trimming. The trade-off is that
every command must be statically known at compile time.

### The parameters class is the single source of truth

A command, its handler, its options, and its arguments are all declared from one attributed
class rather than spread across manual builder calls. Hand-written builder wiring was rejected
as verbose and easy to let drift out of sync. This keeps a command's definition in one place.

### Nullability drives option and argument requiredness

Requiredness and arity are inferred from property nullability and type rather than always
requiring explicit configuration. The C# `required` keyword is deliberately not used because
it is unavailable on every supported target framework. Attribute settings override the
inference where it is wrong.

### Enhanced pipeline with a shared per-execution DI scope

The library substitutes its own option and command actions for the host library's so the
pipeline gains dependency injection, async option handlers, dynamic short-circuiting, and
centralized error handling. The native pipeline cannot provide these — it has no DI, and it
reads its termination flag before execution. A single DI scope per execution is shared by
every option handler and the command handler.

Tasks: [scommandline-preaction-cancellation-bug.md](scommandline-preaction-cancellation-bug.md)

### Input transformation preferred over CommandContext

Passing data from an option handler to a command handler uses the type-safe option-handler
transformation form. Storing values in the command context is supported but discouraged as a
fallback, because it is untyped and hides the dependency between stages.

### Core multi-targets netstandard2.1 and the current .NET

The guiding rule is not to be more restrictive than the dependency being wrapped, so the core
library tracks `System.CommandLine`'s own target frameworks: a netstandard floor plus a modern
leg. The floor stays at 2.1 — dropping to 2.0 to reach .NET Framework was investigated and
rejected as too costly to support. A library target framework is a floor, not a runtime
mandate, so keeping both legs maximizes reach at no cost: older consumers resolve the
netstandard asset, current ones get the better surface.

### Core tracks the current Microsoft.Extensions.Hosting

Core and Defaults reference the current major line of Hosting rather than holding at an older
one to keep the transitive floor low. The lower pin was rejected on lifecycle grounds — the
runtimes this library targets are moving forward regardless. The accepted consequence is that
a consumer on an older runtime resolving the netstandard asset is pulled up with it.

### Commands own stdout; there is no built-in verbosity option

Clean stdout and stderr are the default, and each command controls what it writes.
Automatically adding a recursive verbosity option and console logging to every application was
rejected: a CLI whose job is to produce parseable output should not carry a logging flag it
never asked for, nor emit log lines by default. Logging, and any option that drives it, is
explicit opt-in.

### The library never forces cross-cutting options on consumers

No package registers a recursive or global option on the consumer's behalf — this covers both
verbosity and the output package's own query and compact options, which ship as types the
consumer chooses to apply. A dedicated API for registering recursive options was considered
and found unnecessary: the root command is already public and reachable before parsing, which
is all a consumer needs. The library supplies capability; the consumer sets policy.

### Default logging writes to a file, not the console

The Defaults package configures Serilog with a rolling file sink and no console sink, so
stdout and stderr belong entirely to the command's own output. Console logging was rejected as
a default because log lines and program output must not share a stream — mixing them corrupts
the output contract for anything parsing it. A code baseline owns the sink and its path, and a
configuration section layers over it so levels are tunable at deploy time without recompiling;
a dedicated logging config file was rejected as one more artifact to place with no functional
gain.

### Output is a contract for humans and automation

A command's stdout is a consumed contract — piped to other tools, queried, and fed to agent
tooling as well as read by people — so the library favors a stable, correct schema over visual
compactness, and commands write through a standard output surface rather than ad-hoc console
writes. The governing rules:

- Compaction must be lossless; the moment it changes meaning it is the wrong lever.
- Nulls drop, but empty collections and default values are kept, so nothing has to be
  inferred from a missing key.
- Per-consumer shaping happens at the query layer, not by mutating the canonical output.
- Color is rendered only for a terminal and stripped otherwise, so it never harms the
  machine-readable path.

Tasks: [json-parse-error-and-help-output.tsk.md](json-parse-error-and-help-output.tsk.md)

### The CommandOutput envelope is the output package's core

`Albatross.CommandLine.Outputs` is built around one standard result record that every command
emits for both success and failure, so consumers see a single predictable shape. A shared
serializer and a render surface with query and compact options realize the output contract.
The package carries the output-only dependencies so the core library stays light.

Tasks: [cancellable-print-output.tsk.md](cancellable-print-output.tsk.md), [json-parse-error-and-help-output.tsk.md](json-parse-error-and-help-output.tsk.md)

### The global error handler is opt-in and dependency-light

The output package ships a generic error handler that renders the standard envelope to stderr
and returns a failure exit code, but the consumer must register it. Shipping no handler at all
was rejected as leaving every application to rewrite the same code; auto-wiring one was
rejected as forcing an opinion. Behaviour that would require extra dependencies — semantic
exception unwrapping, reporting the log folder — is deliberately left to consumer subclasses.

### Outputs and Defaults stay separate packages

The output and logging-defaults packages are siblings rather than one merged package, despite
both being opinionated and pairing naturally. Merging was rejected because the coupling is
semantic rather than technical and the two dependency graphs are disjoint and heavy: a merge
would force a logging stack onto consumers who want only the output contract, and the output
contract onto consumers who want only logging defaults. They are documented as a recommended
pairing instead.

### Query and compact options stay in Outputs

The query and compact options live with the output package rather than moving to the
reusable-inputs package. They have no meaning outside the render path, and the query option
carries a JmesPath dependency that would otherwise land on a package kept deliberately light.
Same principle as keeping Outputs and Defaults apart: one package's dependency graph is not
forced onto another's consumers.

### Unified error contract without reserved exit codes

Every error condition — command handler, option handler, service resolution, and cancellation
— is captured as an error record carrying its source and key, and routed as a set to the
registered error handler. Mapping outcomes to reserved exit codes was rejected: the
distinction those codes carried now lives in the error's source, so the handler, not the exit
code, is where an outcome is explained. Exit codes collapse to whatever the handler returns,
or a single default. Errors are logged at their catch site, so the handler only notifies.

Tasks: [route-option-handler-exception-to-error-handler.tsk.md](route-option-handler-exception-to-error-handler.tsk.md)

### Ship a companion Roslyn analyzer

`Albatross.CommandLine.CodeAnalysis` surfaces attribute misuse as IDE diagnostics rather than
letting it appear as cryptic generated-code compiler errors or runtime failures. It is
published and referenced opt-in rather than bundled, so a consumer adds it deliberately.

Tasks: [detect-duplicate-option-names.tsk.md](detect-duplicate-option-names.tsk.md)

### The source generator is bundled into the core package

The generator ships inside the core package's analyzer folder, so referencing the core package
auto-activates code generation. A standalone package was rejected on two grounds: the generator
emits code that depends on core's types, so it has no coherent standalone consumer; and
publishing both would let a consumer reference each and double-load the generator. The
generator project still exists and is consumed as a project reference in-repo.

Tasks: [bundle-codegen-into-core-package.tsk.md](bundle-codegen-into-core-package.tsk.md), [write-generator-bundling-article.tsk.md](write-generator-bundling-article.tsk.md)

### v9 batches breaking changes into one major release

Behaviour changes learned from real-world use of v8 are batched into a single major version
bump rather than absorbed piecemeal across minor releases. One boundary gives consumers one
migration to plan and one unambiguous signal that behaviour changed.

### v9 builds on the System.CommandLine v3 prerelease

The major version bump is the opportunity to move off the v2 line onto the v3 prerelease.
Taking a prerelease dependency is an accepted trade-off because the major-version boundary
already signals breaking change to consumers. The prerelease API must be re-verified before it
reaches GA, since the surface can still shift.

### v9 ships on the prerelease channel until v3 is GA

Because v9 depends on a prerelease of `System.CommandLine`, v9 itself publishes only as a
prerelease. Holding all releases until GA was rejected — development, breaking changes, and
feedback proceed continuously on the prerelease channel, and only the stable tag is gated.
v8.x remains the current stable line for consumers who cannot take a prerelease dependency.

## Open Questions

- Should the Defaults package create the log directory itself, or rely on the consumer and the
  file sink to create it?
- Do any planned analyzers remain unimplemented, beyond the known gap in duplicate-option-name
  detection tracked in
  [detect-duplicate-option-names.tsk.md](detect-duplicate-option-names.tsk.md)?
- The `System.CommandLine` v3 API needs re-verification before it reaches GA; the prerelease
  surface can still shift.

## Dependencies & Constraints

- A stable 9.0.0 cannot ship until `System.CommandLine` v3 reaches GA (expected around
  November 2026, and not controlled by this project). Until then v9 is published only on the
  prerelease channel.
- The core library multi-targets `netstandard2.1` and the current .NET, tracking
  `System.CommandLine`'s own targets. Do not raise the netstandard floor to 2.0 (investigated
  and rejected) and do not drop the netstandard leg. Modern and heavier dependencies belong in
  the Defaults package.
- Reusable `Option<T>` subclasses must expose a `(string name, params string[] aliases)`
  constructor and reusable `Argument<T>` subclasses a `(string name)` constructor — the source
  generator relies on these signatures to instantiate them.
- All commands must be statically declared via attributes at compile time, a consequence of
  choosing source generation over reflection.
- Configuration and logging setup must run after parsing, because they read parsed values.
- Option and argument names derive from kebab-cased property names; property names that
  collide case-insensitively break generation.
- Consumers on the `2.0.0-beta4` API surface must follow the maintained migration guide; the
  beta APIs it covers no longer exist.
