# Write a docfx article: bundling a source generator into a multi-targeted library package

status: new
created: 2026-07-10T00:37:18-04:00
priority: normal
tags: docs docfx packaging codegen msbuild nuget
----

## Objective

Write a docfx article that documents — end to end — how to ship a Roslyn source generator *bundled
inside* a library's own NuGet package, so a consumer gets the generator automatically by referencing
the library, **and** have it work when the library **multi-targets** (e.g. `netstandard2.1;net10.0`).
This is deliberately generalized (not Albatross-specific): the goal is the blog-post/reference that
does not currently exist anywhere. As of a search ~mid-2025 there was **no single blog post or
Microsoft doc** covering the multi-target pack interaction; that gap is the reason to write it.

Audience: a .NET library author who has a working source generator project and wants consumers to get
it via a normal `PackageReference` to the library, without publishing the generator as a separate
package.

## Reasoning

This repo is the **reference implementation** — everything the article describes is proven and
shipping here. The article should be written by reading these artifacts (they are the source of
truth; verify every claim against them and by packing):

- `Albatross.CommandLine.CodeGen/CodeGen.Payload.targets` — the shared payload file. Its header
  comment already explains the core mechanics; the article is essentially the long-form of that.
- `Albatross.CommandLine/Albatross.CommandLine.csproj` — the *library* side: multi-targets
  `netstandard2.1;net10.0`, sets `BundleCodeGenPayloadOnly=true` and `CodeGenPayloadPackTfm=net10.0`,
  imports the shared targets, and references the generator as an analyzer-style ProjectReference.
- `Albatross.CommandLine.CodeGen/Albatross.CommandLine.CodeGen.csproj` — the *generator* side:
  imports the same shared targets (compiles against the deps), keeps `Microsoft.CodeAnalysis.CSharp`
  out of the payload, and carries the `GetDependencyTargetPaths` target for the project-reference path.
- `project-mgmt/bundle-codegen-into-core-package.tsk.md` — the full investigation, both gotchas, and
  the verification method (pack + unzip + consumer smoke test).
- `.projects` — how publishing is gated (the generator is simply omitted from this list; see the
  project.md decision "The source generator is bundled into the core package, not published standalone").

### The non-obvious findings the article must center on (these are the value)

1. **Outer vs. inner build.** `GeneratePathProperty`'s `$(Pkg*)` values resolve only in the per-TFM
   **inner** build, but `dotnet pack` collects static `<None ... Pack="true">` content at the
   **outer** (no-`TargetFramework`) evaluation. So on a multi-targeted project the naive `<None
   Include="$(PkgFoo)\lib\...\*.dll" Pack="true">` items silently pack **nothing** — no warning, no
   error. The same items work fine on a single-target project, which is why this is a trap. The fix
   is a `TargetsForTfmSpecificContentInPackage` target that runs in the inner build (where `$(Pkg*)`
   is live) and emits `TfmSpecificPackageFile` items — **guarded to a single TFM** so a multi-target
   project doesn't add the files twice (NU5118 duplicate-file).
2. **`IncludeAssets="none"` suppresses `GeneratePathProperty`.** To reference a package only to get
   its extracted path (not consume its assemblies), use `ExcludeAssets="compile;runtime"`, not
   `IncludeAssets="none"` — the latter leaves `$(Pkg*)` empty.

### Supporting points to cover

- **Two delivery paths, both needed if you develop in a monorepo:**
  - *Package consumers* get the generator via the bundled `analyzers/dotnet/cs` payload.
  - *In-repo project references* get the generator's **dependencies** via the
    `GetDependencyTargetPaths` / `TargetPathWithTargetPlatformMoniker` (`IncludeRuntimeDependency="false"`)
    trick — independent of packaging. Explain both and that they don't overlap.
- **Analyzer-style ProjectReference is mandatory for multi-target:** `OutputItemType="Analyzer"` +
  `ReferenceOutputAssembly="false"`. A plain ProjectReference leaks the generator's deps into the
  library's compile; on `net10` that collides with the BCL (`error CS0433`, e.g. a polyfilled
  `NotNullWhenAttribute`). Note this also suppresses the package dependency, which is *why* bundling
  (not a dependency) is the delivery mechanism.
- **Don't also publish the generator standalone** — referencing both the library (bundled) and the
  standalone generator double-loads it → duplicate generated types. Show gating publish via a project
  list / `IsPackable`.
- **Roslyn itself is host-provided** — never pack `Microsoft.CodeAnalysis.*` into the payload.
- **Verification recipe:** `dotnet pack` → `unzip -l` the `.nupkg` and confirm the full payload under
  `analyzers/dotnet/cs`; then a throwaway consumer project with `EmitCompilerGeneratedFiles=true`
  referencing the packed `.nupkg` from a local feed, confirming the generated `.g.cs` appears.
  (Watch the edge case: a generator that emits into the annotated type's namespace throws
  `Invalid namespace identifier: ''` if that type is in the global namespace — put fixtures in a namespace.)

### Article structure (single-target first, then multi-target — this is the spine)

Lead with a working **single-target** example, then break it by switching to **multi-target**. The
"why does the obvious approach pack nothing?" reveal is the hook, and it maps onto real projects in
this repo (use them as the worked samples):

1. **Single-target generator library (works).** A `netstandard2.0` library that bundles the generator
   DLL + its deps into `analyzers/dotnet/cs` with static `<None Include="$(PkgFoo)\lib\netstandard2.0\*.dll"
   Pack="true" PackagePath="analyzers/dotnet/cs">` items backed by `GeneratePathProperty="true"`
   PackageReferences. `dotnet pack` → `unzip -l` shows the full payload. This naive approach genuinely
   works. Real example: **`Albatross.CommandLine.CodeGen`** (its own package).
2. **Switch to multi-target — the exact same items now pack nothing.** Change
   `<TargetFramework>net…</TargetFramework>` to `<TargetFrameworks>netstandard2.1;net10.0</TargetFrameworks>`,
   pack, `unzip -l` — the `analyzers/` payload is gone, **with no warning or error**. This is the trap.
   Explain the cause: `$(Pkg*)` resolve only in the per-TFM inner build, but pack collects static
   `<None>` content in the outer (no-TFM) build. Real example: the core lib bundling the generator.
3. **The fix.** Replace the static `<None>` items with a `TargetsForTfmSpecificContentInPackage`
   target that emits `TfmSpecificPackageFile` from the inner build, **guarded to one TFM** (NU5118),
   and switch any `IncludeAssets="none"` to `ExcludeAssets="compile;runtime"` (so `GeneratePathProperty`
   still resolves). Pack, `unzip -l` — payload restored. Then show factoring it into a shared
   `.targets` imported by both the generator project and the library, so both share one payload
   definition. Real example: `CodeGen.Payload.targets` + the two csproj imports.

Each of the three should be a copy-pasteable snippet plus the `unzip -l` output (present → empty →
present) so the reader *sees* the failure and the fix, not just reads about them.

**Let the diff do the teaching.** The single→multi step must be shown as a *minimal one-line diff*
(`<TargetFramework>net10.0</TargetFramework>` → `<TargetFrameworks>netstandard2.1;net10.0</TargetFrameworks>`,
nothing else changed) sitting directly above the before/after `unzip -l`. Seeing that one line make
the whole `analyzers/dotnet/cs` payload disappear — with no other edit and no warning — explains the
outer-vs-inner-build behavior on its own; the prose that follows just names what the reader already
saw happen. Same technique for the fix: show it as a diff from the broken state, then the restored
`unzip -l`. Minimize prose; maximize side-by-side before/after.

### Placement / mechanics

- New file under `docfx_project/articles/` (e.g. `bundling-source-generator-multitarget.md`).
- Register it in `docfx_project/articles/toc.yml`.
- Cross-link from `code-generator.md` (and optionally `whats-new-v9.md`, since v9 is where this repo
  adopted it).
- Keep code snippets copy-pasteable and generalized; use this repo's files as the worked example but
  don't assume Albatross-specific names in the general guidance.

## Conclusion

(pending)
