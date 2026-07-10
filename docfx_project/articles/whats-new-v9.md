# What's New in v9

v9 is a **breaking major release**. After v8 stabilized and was used across several projects, a set of recurring lessons accumulated — most of them about *defaults*. Rather than absorb the changes piecemeal, they are batched into one major version bump (8.x → 9.0), which also serves as the opportunity to move onto the **System.CommandLine v3** line.

This article explains what changed and, more importantly, *why*. For task-level how-to, follow the links in each section.

> [!IMPORTANT]
> **v9 ships as a prerelease.** Because it depends on the System.CommandLine v3 prerelease, v9 is published only on the prerelease channel (e.g. `9.0.0-preview.*`). A stable `9.0.0` cannot ship until System.CommandLine v3 reaches GA (expected ~November 2026). **v8.x remains the current stable line** for consumers who cannot take a prerelease dependency.

## The theme: the library sets fewer defaults

The unifying idea behind v9 is that a library should provide **capabilities, not policies**. v8 made several decisions on the consumer's behalf — every CLI got a `--verbosity` option, logging went to the console, output was ad-hoc. Those are convenient until they are wrong, and when they are wrong they are hard to opt out of. v9 removes the imposed policies and lets each application choose.

---

## 1. Clean `stdout`/`stderr` by default

**What changed:** The core library no longer creates the recursive global `--verbosity`/`-v` option, and it no longer configures any logging by default. With no setup, a command writes nothing except its own output.

**Why:** v8 added `--verbosity` to every command on the assumption that every CLI should emit some logging. That is convenient in enterprise settings where the quickest way to build a CLI is to log to the console — but it is a poor *default*. A CLI whose job is to produce clean, parseable output should not carry a logging flag it never asked for, nor emit log lines that corrupt its output. In v9 the command owns its output; logging is explicit opt-in.

**Impact / migration:** Any v8 consumer that relied on the automatic `--verbosity` option or default console logging must now opt in (see [Logging & Verbosity](logging-verbosity.md)). The `VerbosityOption` type has been removed.

## 2. System.CommandLine v3

**What changed:** The core library moves from `System.CommandLine` 2.0.2 to the 3.0 prerelease, and now multi-targets `netstandard2.1;net10.0` (tracking System.CommandLine's own target frameworks — a netstandard floor for reach plus a modern `net10.0` leg). The `net10.0` leg is AOT/trim-analyzed (`IsAotCompatible`).

**Why:** A major version bump already signals breaking change to consumers, so it is the right moment to adopt the next System.CommandLine line. A source-level comparison found **no breaking changes to Albatross's usage** — at the API level the migration currently looks like a reference bump. System.CommandLine v3 itself ships `netstandard2.0` + `net10.0`; core keeps a `netstandard2.1` floor for broad reach and adds a `net10.0` leg to match v3's modern target, so the broad-compatibility stance holds. (The prerelease API can still shift before GA; this is re-verified as it evolves.)

## 3. File-based logging in `Albatross.CommandLine.Defaults`

**What changed:** `WithDefaults()` / `WithSerilog()` now configure Serilog to write to a **daily-rolling file** under `IApplicationPath.LogRoot`, with **no console sink**. The `Albatross.Logging` dependency was dropped in favor of referencing Serilog directly.

**Why:** This is the complement of clean-output-by-default. Log lines and command output must not share a stream — mixing them corrupts the output contract for anything parsing it. Sending logs to a file keeps `stdout`/`stderr` entirely for the command.

**How verbosity works now:** The level is no longer a CLI flag. `WithSerilog()` sets a code baseline (`Information` + `FromLogContext` + `WithThreadId` + the file sink) and then layers `ReadFrom.Configuration` on top, so a `Serilog` section in `appsettings.json` can raise/lower the level, add per-namespace overrides, and add sinks — editable at deploy time without recompiling. Details in [Logging & Verbosity](logging-verbosity.md) and the [Defaults Library](defaults-library.md).

**Impact / migration:** Consumers must register an `IApplicationPath` (from `Albatross.Config`) before building the host — `WithSerilog()` throws a guiding exception otherwise. Change level tuning from `-v Debug` to a `Serilog:MinimumLevel` setting.

## 4. A first-class command output strategy: `Albatross.CommandLine.Outputs` (new package)

**What changed:** A new opt-in package formalizes how commands emit results, centered on a `CommandOutput` / `CommandOutput<T>` envelope. It is **JSON-first with built-in JmesPath querying**: a shared serializer, a `Print` surface, and reusable `--query` / `--compact` options.

**Why:** Once `stdout` is clean and logging is off the console, a command's output *is* its result — routinely piped to `jq`, queried with JmesPath, and fed to LLM/agent tooling as well as read by humans. That makes output a **contract**, and the package encodes the principles that keep it stable:

- Output is a contract; **compaction must be lossless** — the moment it changes meaning, it is the wrong lever.
- **Drop nulls** (present-and-null equals absent) but **keep empty arrays/objects and default values**, so scripted/JmesPath/AI consumers never have to infer a missing key.
- Shape less data per call at the **query layer (JmesPath)** rather than mutating the canonical output.
- **Color is free and non-destructive** — rendered for a TTY, ANSI stripped when piped.

The package also ships a simplified, dependency-light, **opt-in** `GlobalErrorHandler` that reports failures through the same `CommandOutput` envelope on `stderr`.

**Why a separate package (not merged into Defaults):** `Outputs` (JSON/JmesPath/Spectre) and `Defaults` (Serilog/config) are orthogonal opinionated concerns with disjoint, heavy dependency graphs. Merging would force Serilog onto consumers who only want structured output, and vice versa. They are documented as a recommended pairing instead.

## 5. Opt-in options, never auto-registered

**What changed:** The library's own cross-cutting options — `--query` and `--compact` from `Outputs` — are provided as reusable option *types*. The consumer decides whether and where to apply them (e.g. `[UseOption<QueryOption>]`, or on a shared base-params class); they are never auto-registered as recursive.

**Why:** This is the `--verbosity` lesson generalized: v9 never forces a global/cross-cutting option on consumers. If you want a recursive option, you add it deliberately.

**No new API required.** Adding a recursive option uses existing extensibility — `CommandBuilder.RootCommand` is public. Create an `Option<T>` with `Recursive = true` and add it to `host.CommandBuilder.RootCommand` before `Parse()`. (An early plan floated a dedicated "facility" for this; it turned out to be unnecessary.) See [Logging & Verbosity](logging-verbosity.md#adding-your-own-recursive-option) for a worked `--verbosity` example.

## 6. Dependency stance: System.CommandLine v3 and Hosting 10.x

**What changed:** Core moves to `System.CommandLine` v3 (prerelease) and raises `Microsoft.Extensions.Hosting`/`DependencyInjection` to **10.x** (10.0.9); `Albatross.CommandLine.Defaults` matches.

**Why:** The original v9 plan held Hosting at 8.0.1 to avoid raising the transitive floor and forcing consumers onto Hosting v10. That was reversed: .NET 8 reaches EOL in Nov 2026 (≈ v9's own GA timing) and .NET 9 is already EOL, so the runtimes this library targets are moving to 10.x regardless. **Trade-off accepted:** because core still ships the `netstandard2.1` leg, a net8/net9 consumer resolving that asset is pulled up to Hosting 10.x — acceptable given those runtimes are EOL/near-EOL. Core builds clean on both the `netstandard2.1` and `net10.0` legs with Hosting 10.x (which still ships a netstandard-compatible asset).

---

## Migration checklist (v8 → v9)

- [ ] Expect a **prerelease** package version; keep v8.x if you cannot take a prerelease dependency.
- [ ] Remove any reliance on the automatic `--verbosity`/`-v` option or `CommandBuilder.VerbosityOption`.
- [ ] If you want logging, add `Albatross.CommandLine.Defaults`, register an `IApplicationPath`, and call `WithDefaults(appPath.ConfigRoot)`. Logs now go to a **file**, not the console.
- [ ] Move log-level tuning from the command line to a `Serilog:MinimumLevel` section in `appsettings.json`.
- [ ] If you print structured results, adopt `Albatross.CommandLine.Outputs` (`CommandOutput` + `Print` + `--query`/`--compact`) instead of ad-hoc `Console.WriteLine`.
- [ ] Verify your build against the System.CommandLine v3 prerelease (expected to be a reference bump).
- [ ] If you referenced `Albatross.CommandLine.CodeGen` directly, **remove that package reference** — the source generator is now bundled into `Albatross.CommandLine`, so referencing the core package is enough (keeping both would double-load the generator). `Albatross.CommandLine.CodeAnalysis` stays an opt-in package you add explicitly.
- [ ] Note that core now pulls in **`Microsoft.Extensions.Hosting` 10.x**; a .NET 8/9 app referencing core will get Hosting 10.x transitively.

## Related articles

- [Logging & Verbosity](logging-verbosity.md)
- [Defaults Library](defaults-library.md)
- [Custom Global Error Handler](custom-error-handler.md)
