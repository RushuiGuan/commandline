# Detect duplicate effective option names in the analyzer (extend ACL0001)

status: started
created: 2026-07-09T08:28:09-04:00
priority: normal
tags: analyzer code-analysis acl0001 diagnostics
----

## Objective

Fix `ACL0001` (`DuplicateOptionNameAnalyzer`) so it decides duplicates by the **effective option name**
each property resolves to, not by the property name. The current property-name comparison is wrong in
both directions: it **misses** real duplicates that arise from `[UseOption<T>]` reuse / `[DefaultNameAliases]`
/ explicit aliases (false negatives) **and** it **fires on valid code** whose property names collide
case-insensitively but whose option names differ (false positive). Missed duplicates compile and generate
cleanly, then throw at runtime on the first name-based lookup (`System.CommandLine`: "Command X has more
than one child named ..."; see the `system-commandline-duplicate-names` article); false positives block
legitimate code with a bogus warning.

## Empirical evidence (2026-07-09)

Fixtures in `Albatross.CommandLine.Test/CodeGen/CodeAnalysis_DupOptionName.cs` encode the intended truth
table; building the test project (analyzer referenced as a project analyzer) produced:

| Verb / class | Options → effective names | Expected | Actual |
|---|---|---|---|
| `dup-option-name` | `MyTest`,`myTest` `[Option]` → `--my-test`,`--my-test` | warn | ✅ fired |
| `dup-option-name2` | `MyTest` `[Option]`, `MyTest1` `[UseOption<MyTest1Option>]` → `--my-test`,`--my-test` | warn | ❌ missed (false negative) |
| `dup-option-name3` | `MyTest1`,`MyTest2` both `[UseOption<…>]` w/ `[DefaultNameAliases("--my-test")]` → `--my-test`,`--my-test` | warn | ❌ missed (false negative) |
| `dup-option-name4` | `MyTest`,`mytest` `[Option]` → `--my-test`,`--mytest` | no warn | ❌ fired (false positive) |

Root cause: `DuplicateOptionNameAnalyzer.AnalyzeNamedType` groups by `p.Name.ToUpperInvariant()` (property
name) and only inspects `[Option]` properties. Computing the effective name per property and grouping on
that fixes all four cases at once (case 4 stops firing because `--my-test` ≠ `--mytest`).

## Reasoning

### Why this exists / how it was found

The generator deliberately leaves duplicate option names in place — see the comment in
`Albatross.CommandLine.CodeGen/CommandClassBuilder.cs` ("duplicate aliases are left alone since
System.CommandLine will ignore ... makes it easier for the developers to fix their mistakes"). So the
*intended* place to catch the mistake is the analyzer, not the generator.

This surfaced while adding parse-and-bind tests: the `test explicit-option` fixture
(`Albatross.CommandLine.Test/CodeGen/TestExplicitOptionClassAndHandler.cs`) has three properties
—`ExplicitOptionWithoutHandler`, `ExplicitOptionWithItsOwnSetup`, `ExplicitOptionWithAliasOverride`—
that all reuse `[UseOption<OptionWithoutHandler>]`, and `OptionWithoutHandler` carries
`[DefaultNameAliases("--option-without-handler", "--wo")]`. All three therefore resolve to the same
option name `--option-without-handler`. It builds and passes structural tests, but any run that builds
the command tree and reads a value by name throws in `SymbolResultTree.PopulateSymbolsByName`. That
fixture is now annotated as intentional (structural-only) and points here.

### What the existing analyzer does — and its two gaps

`Albatross.CommandLine.CodeAnalysis/DuplicateOptionNameAnalyzer.cs` (`ACL0001`) today:
- Only inspects properties whose attribute is `OptionAttribute` (`HasOptionAttribute` matches metadata
  name `OptionAttribute` only).
- Groups by the **property name**, upper-cased (`GroupBy(p => p.Name.ToUpperInvariant())`).

Gaps that let the real duplicate through:
1. **Ignores `[UseOption<T>]`.** The failing case uses `UseOptionAttribute<T>`, which `HasOptionAttribute`
   does not match — so those properties are never even considered.
2. **Groups by property name, not by the effective option name.** The CLI name is not always the
   property name. Two *differently named* properties can still collide, e.g.
   - `[Option("--foo")] A` and `[Option("--foo")] B` — wait, note `[Option]` ctor args are *aliases*,
     but aliases share the same "child name" space as the primary name at runtime, so alias collisions
     count too;
   - multiple `[UseOption<T>]` reusing a type whose `[DefaultNameAliases("--x")]` fixes the primary name
     (the observed case);
   - a `[UseOption<T>]` DefaultNameAliases name that equals another property's kebab-cased name.

### The source of truth to mirror

The analyzer must compute the same effective name the generator emits. That logic lives in
`Albatross.CommandLine.CodeGen/IR/CommandOptionParameterSetup.cs` (and its base
`CommandParameterSetup`). Summary of how the **primary name (`Key`)** and **aliases** are derived:

- Default primary name = kebab-cased property name, prefixed with `--` (confirmed by existing tests:
  property `RequiredStringOption` → `--required-string-option`).
- `[Option("a","ab")]` ctor args are **aliases** (`-a`, `--ab`) via `CreateAlias` (1 char → `-x`,
  longer → `--x`, already-dashed → as-is); they do **not** change the primary name.
- `[UseOption<T>]` where `T` has `[DefaultNameAliases(name, aliases...)]` **and** `UseCustomName` is not
  set → primary name = that `name`, aliases = the rest. `UseCustomName = true` ignores DefaultNameAliases
  and falls back to the property-derived name.
- Final: if the primary name doesn't start with `--`, prefix `--`.

A collision at runtime is any repeat in the **union of {primary name} ∪ {aliases}** across all option
properties of the command (System.CommandLine's "child named" space covers both). ACL0001 today only
looks at a subset of primary names. The fix is to compute the full effective name+alias set per option
property and report any name used by more than one property.

### Scope notes / constraints

- **Inherited options matter.** The generator flattens options from `BaseParamsClass`
  (`CommandSetup.GetCommandParameters` uses `GetDistinctProperties(true)`), so mutually-exclusive params
  sets sharing a base can contribute colliding options. The analyzer should consider inherited option
  properties, not just those declared on the annotated type.
- **Drift risk.** The analyzer (`netstandard2.0`, no reference to CodeGen) would re-implement the name
  logic, which can drift from `CommandOptionParameterSetup`. Prefer extracting the name/alias
  computation into a small shared helper both projects can reference; if that's impractical across the
  assembly boundary, at minimum add tests that pin the two implementations to the same expectations.
- **Rule id decision (recommendation, confirm with maintainer):** broaden `ACL0001` to key on the
  effective name/alias set (case-only property collision becomes a special case of it) and include
  `[UseOption<T>]`. This changes ACL0001's message wording/semantics, which is acceptable in the v9
  prerelease. Alternatively add a distinct id (e.g. `ACL0005`) if keeping the two messages separate is
  preferred. Note `project.md` and `ai-*` docs currently describe ACL0001 as the duplicate-name rule;
  update them to match whatever is chosen.
- Arguments are out of scope here (positional; `ConflictingOptionArgumentAnalyzer` covers a different
  concern). This task is options only.

### Files

- `Albatross.CommandLine.CodeAnalysis/DuplicateOptionNameAnalyzer.cs` — the analyzer to extend.
- `Albatross.CommandLine.CodeGen/IR/CommandOptionParameterSetup.cs` + `CommandParameterSetup.cs` —
  the name/alias resolution to mirror (or extract into a shared helper).
- `Albatross.CommandLine/Annotations/{OptionAttribute,UseOptionAttribute,DefaultNameAliasesAttribute}.cs`
  — the attributes whose args feed the effective name.
- `Albatross.CommandLine.Test/CodeGen/TestExplicitOptionClassAndHandler.cs` — the documented
  intentional-duplicate fixture that motivates this; a positive analyzer test should assert ACL fires
  on an equivalent shape.

### Testing

Add analyzer unit tests (Microsoft.CodeAnalysis.Testing / Verifier pattern) covering: two `[Option]`
props with case-only name difference (existing behavior still fires); two `[UseOption<T>]` sharing a
DefaultNameAliases name (the observed case); alias-vs-name collision; `UseCustomName = true` avoiding a
false positive; inherited option from a `BaseParamsClass` colliding with a derived one; and a clean
class producing no diagnostic. Check whether the CodeAnalysis project currently has an analyzer test
harness; if not, standing one up is part of this task.

## Conclusion

**Primary-name detection fixed (2026-07-09).** `DuplicateOptionNameAnalyzer` now groups by the effective
CLI name instead of the property name. It resolves each `[Option]` / `[UseOption<T>]` property to the name
the generator would emit — kebab-cased property name, or `[DefaultNameAliases]` on the option type unless
`UseCustomName` is set — via a hand-mirrored copy of Humanizer's `Kebaberize()` (documented in the source
as a sync point). Verified against `CodeAnalysis_DupOptionName.cs`: the two false negatives
(`dup-option-name2`, `dup-option-name3`) now warn, the false positive (`dup-option-name4`, `--my-test` vs
`--mytest`) no longer does, and all 65 tests pass. The diagnostic message now reports the resolved name;
`code-analysis.md` was updated to match.

**Still open (not done in this pass):**
- **Alias-level collisions** — detection compares primary names only; a name-vs-alias or alias-vs-alias
  collision (both are real `System.CommandLine` "child name" conflicts) is not yet caught.
- **Inherited options** — options contributed by a `BaseParamsClass` (the generator flattens them via
  `GetDistinctProperties(true)`) are not considered; only declared members are scanned.
- **Verifier harness** — the fixtures live in the compiled test project and emit warnings on every build
  (including the intentional-duplicate `ExplicitOption.cs`). They should migrate to a
  `Microsoft.CodeAnalysis.Testing` verifier that asserts expected diagnostics instead of surfacing them.
- **Kebaberize drift** — the hand-copied algorithm should be pinned by tests against the generator's real
  `Kebaberize()`, or the name logic extracted into a helper shared by both projects.
