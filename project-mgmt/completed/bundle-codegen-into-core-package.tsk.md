# Bundle the source generator into the core package so consumers auto-get code generation

status: completed
created: 2026-07-09T23:22:40-04:00
priority: high
tags: packaging codegen analyzers nuget multi-target
----

## Objective

Make a consumer that references the **`Albatross.CommandLine`** NuGet package automatically get
the `Albatross.CommandLine.CodeGen` source generator (so their `[Verb]` classes generate command
wiring) — without reintroducing the net10 compile break and without leaking the generator as a
runtime dependency. Deliver this by **bundling** the generator payload into the core package under
`analyzers/dotnet/cs`, driven from a single shared MSBuild file so the payload list cannot drift
from `CodeGen.csproj`.

## Reasoning

### Why the current state is broken

Core now multi-targets `netstandard2.1;net10.0` (see project.md → Key Design Decisions). To make
that compile, the ProjectReferences to `CodeGen`/`CodeAnalysis` had to become analyzer-style
(`OutputItemType=Analyzer` + `ReferenceOutputAssembly=false` + `PrivateAssets=all`). This is
**required**: a plain ProjectReference leaks `Albatross.CodeAnalysis.Polyfill` into core's compile,
and on net10 its `NotNullWhenAttribute` collides with the BCL → `error CS0433`. Verified 2026-07-09.

Side effect (verified by packing): `ReferenceOutputAssembly=false` suppresses the package
dependency, so the packed core nuspec lists only `Microsoft.Extensions.Hosting` + `System.CommandLine`
— **neither CodeGen nor CodeAnalysis reaches consumers.** (The committed HEAD, ns2.1-only + plain
refs, *did* list them as dependencies, but with `exclude="Build,Analyzers"`, so even that likely did
not auto-activate the generator.) `PrivateAssets` is **not** the cause — removing it alone changed
nothing; `ReferenceOutputAssembly=false` is what drops the dependency. And you cannot set it back to
`true` to restore the dependency, because that re-triggers `CS0433`.

A first bundling attempt in `Albatross.CommandLine.csproj` (`<None Include="$(PkgFoo)\lib\...">` items)
packs nothing: those `$(Pkg*)` properties are only created by `<PackageReference GeneratePathProperty="true">`
declared *in the same project*, which live in `CodeGen.csproj`. In core they are empty
(`PkgHumanizer_Core = ""`), so the globs match zero files. Verified: pack produced only `lib/**`, no
`analyzers/`.

### Why bundling (not a package dependency)

Two ways to deliver a generator to consumers: (A) bundle its DLLs into the library package's
`analyzers/dotnet/cs`; (B) declare a package dependency on the separately-published CodeGen package
with analyzer assets flowing (`PackageReference … IncludeAssets="analyzers;build"
ExcludeAssets="compile;runtime" PrivateAssets="none"`). (B) couples core's build to a *published*
CodeGen version, which breaks the edit-CodeGen→rebuild-core inner loop during prerelease where both
change together. **Chose (A)** for self-containment and dev-loop reliability.

### The source of truth to mirror

`CodeGen.csproj` already assembles the full payload for its OWN package: it packs
`$(OutputPath)\Albatross.CommandLine.CodeGen.dll` plus seven dependency DLLs pulled from the NuGet
cache via `$(Pkg*)` path properties (Humanizer.Core, Albatross.CodeAnalysis,
Albatross.CodeAnalysis.Polyfill, Albatross.CodeGen, Albatross.CodeGen.CSharp, Albatross.Collections,
Albatross.Text). `Microsoft.CodeAnalysis.CSharp` is deliberately **not** packed (Roslyn is provided
by the host compiler) and must stay excluded. The generator needs its whole payload co-located in
`analyzers/dotnet/cs` to load in the consumer's Roslyn.

### Design

Create `Albatross.CommandLine.CodeGen\CodeGen.Payload.targets` holding, in one place:
- The seven `GeneratePathProperty="true" PrivateAssets="all"` PackageReferences, with a **conditional**
  `<IncludeAssets Condition="'$(BundleCodeGenPayloadOnly)' == 'true'">none</IncludeAssets>` so the
  importer can opt into "resolve the path but don't consume the assets."
- The eight `<None … Pack="true" PackagePath="analyzers/dotnet/cs">` items, with CodeGen.dll referenced
  as `$(MSBuildThisFileDirectory)bin\$(Configuration)\netstandard2.0\Albatross.CommandLine.CodeGen.dll`
  so it resolves to the CodeGen output dir regardless of which project imports the file.

Wire-up:
- `CodeGen.csproj`: import the shared file (no flag → compiles against the deps as today), delete the
  now-moved inline PackageReferences and None items. Keep `Microsoft.CodeAnalysis.CSharp`, the
  `GetDependencyTargetPaths` target (needed for the *project-reference* analyzer flow during local
  builds), and `SuppressDependenciesWhenPacking`.
- `Albatross.CommandLine.csproj`: set `<BundleCodeGenPayloadOnly>true</BundleCodeGenPayloadOnly>`,
  import the shared file, delete the broken inline None group. Keep the analyzer-style
  ProjectReferences as-is.

### Risks / things to verify

- `IncludeAssets="none"` must still let `GeneratePathProperty` resolve `$(Pkg*)` in core (expected:
  yes — the package is still restored). If not, use `ExcludeAssets="all"` instead.
- net10 leg of core must still compile clean (no `CS0433`) after adding the seven references with
  assets excluded.
- Multi-target pack must not emit the analyzer None items twice (NU5118 duplicate-file). If it does,
  guard the None group.
- `CodeGen`'s own package must be byte-for-byte equivalent after the refactor (pack it and diff the
  `analyzers/dotnet/cs` file list).

### Open question

- Should `Albatross.CommandLine.CodeAnalysis` (the ACL analyzers) also be bundled so consumers get the
  diagnostics automatically? Currently it is documented as an opt-in package (see `code-analysis.md`
  install section). This task bundles **only the generator**; CodeAnalysis stays opt-in unless the
  maintainer decides otherwise.

### Verification

Pack core and confirm `analyzers/dotnet/cs` contains CodeGen.dll + the seven deps. Then create a
throwaway consumer that references the packed `.nupkg` (local feed), declares a `[Verb]` class, and
confirm the generated command appears (build succeeds and the generated source is emitted).

## Conclusion

**Done (2026-07-09), verified end-to-end.** The generator payload is now bundled into the core
package under `analyzers/dotnet/cs`, so referencing `Albatross.CommandLine` auto-activates the
source generator for consumers.

Implementation:
- New `Albatross.CommandLine.CodeGen\CodeGen.Payload.targets` — single source of truth for the
  payload (the seven `GeneratePathProperty` dep PackageReferences + the pack logic). Imported by
  both `CodeGen.csproj` (compiles against the deps; packs its own package) and
  `Albatross.CommandLine.csproj` (bundles the same payload). `Microsoft.CodeAnalysis.CSharp`
  stays in `CodeGen.csproj` only (Roslyn is host-provided; never packed).
- Core sets `BundleCodeGenPayloadOnly=true` (→ `ExcludeAssets=compile;runtime` on the deps so they
  restore for their paths but never enter core's compile — avoids the net10 `CS0433`) and
  `CodeGenPayloadPackTfm=net10.0`.

Two non-obvious findings drove the final shape:
1. **`IncludeAssets="none"` suppresses `GeneratePathProperty`** — `$(Pkg*)` came back empty. Had to
   use `ExcludeAssets="compile;runtime"` instead (restores the package, keeps the path, drops the
   compile/runtime assets).
2. **On a multi-targeted project, `$(Pkg*)` only resolve in the per-TFM *inner* build, but
   `dotnet pack` collects static `<None>` content at the *outer* (no-TFM) evaluation** — so static
   `<None Pack>` globs pack nothing (verified: outer eval = 0 items, inner = 18). Switched to a
   `TargetsForTfmSpecificContentInPackage` target that runs in the inner build (paths live) and is
   guarded to one TFM (`CodeGenPayloadPackTfm`) so a multi-targeted importer doesn't double-add
   (NU5118). Single-targeted `CodeGen` defaults that guard to its own TFM, so its package is
   unchanged (verified: identical 8-DLL payload).

Verification:
- Core packs the full 8-DLL payload under `analyzers/dotnet/cs`; no NU5118; net10 + ns2.1 compile
  clean (no `CS0433`). CodeGen's own package byte-list unchanged. Test project (project-reference
  path) still builds and generates.
- **Consumer smoke test** (throwaway net10 project referencing the packed `Albatross.CommandLine`
  9.0.0 from a local feed, with a namespaced `[Verb<GreetHandler>("greet")]` params class): the
  generator fired and emitted `CodeGenExtensions.g.cs` + `GreetCommand.g.cs` with correct content.
  Note: a params class in the *global* namespace makes the generator throw
  `ArgumentException: Invalid namespace identifier: ''` (it emits the partial command into the
  params class's namespace) — a pre-existing generator edge case, unrelated to packaging; worth a
  follow-up guard/diagnostic.

Follow-ups (not done here):
- Decide whether `Albatross.CommandLine.CodeAnalysis` (ACL analyzers) should also be bundled so
  consumers get diagnostics automatically; currently opt-in per `code-analysis.md`. (Note: the core
  csproj's ProjectReference to CodeAnalysis was also removed during this work, so ACL no longer runs
  on core's own build either.)
- Generator should fail gracefully (or emit a diagnostic) when a `[Verb]` params class has no
  namespace, instead of throwing `Invalid namespace identifier: ''`.
