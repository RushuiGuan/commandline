---
name: v9 release checklist
status: started
created: 2026-11-16T11:48:48-05:00
priority: high
tags: release v9 nuget ci
---

## Objective

Ship Albatross.CommandLine 9.0.0: keep publishing release candidates while System.CommandLine v3 is
in prerelease, then cut the stable 9.0.0 once v3 reaches GA. Every release step that CI does not
enforce is listed here so nothing is skipped, and the open items that must be settled before the
stable tag are tracked in one place.

## Design

- Follows the prerelease gate in [Key Design Decisions](project.md#key-design-decisions)
  ("v9 ships as prerelease until it references only stable dependencies") and the matching
  constraint in [Dependencies & Constraints](project.md#dependencies--constraints): every 9.0.0 build
  that references a prerelease package is an `rc`.
- Microsoft package versions follow "Microsoft package references track the latest patch of the
  newest TFM's major" in [Key Design Decisions](project.md#key-design-decisions): latest 10.x, not
  11.x.
- The checklist follows the existing CI flow rather than adding release scripts: `main` is the work
  branch, `rc` produces candidates, tags promote. Steps CI already enforces (version format, rc
  number, branch provenance) are listed only so a red run is understood, not checked by hand.
- Local verification mirrors CI: the same two test projects, in Release. The full solution is not a
  release signal, because `Albatross.CommandLine.CodeAnalysisTest` is a negative fixture that fails
  by design.

## Implementation

### How a release flows

| Action | Result |
|---|---|
| push `main` | Nothing is published except the docs site (`.github/workflows/github-page.yml`) |
| push `rc` | Packs every project under `# packages` in `.projects` and publishes `9.0.0-rc.N` to GitHub Packages; `N` is the commit count of the pushed commit (`v9.0.0-rc.288` sits on commit 288) |
| push tag `v9.0.0-rc.N` | Promotes that exact RC to nuget.org and creates a GitHub prerelease |
| push tag `v9.0.0` | Publishes the stable release to nuget.org and creates a GitHub release |

CI runs `dotnet test -c Release` on every project under `# tests` in `.projects` before packing; a
failing test stops the publish. The workflow logic lives in `RushuiGuan/devops`
(`nuget-prerelease.yml`, `nuget-prerelease-nugetorg`, `nuget-release-nugetorg`, `release-guards`):

- the rc workflow rejects a `<Version>` that is not a clean `X.Y.Z`, and refuses to publish
  candidates once `v9.0.0` exists
- RC promotion checks that the tag's base matches `Directory.Build.props`, that `N` equals the
  tagged commit's commit count, and that the commit is on `rc`
- the stable release checks that the tag matches `Directory.Build.props` and that the commit is on
  `origin/production`

### 1. Prepare on `main`

- [ ] `<Version>` in `Directory.Build.props` is `9.0.0`, and no `v9.0.0` tag exists.
- [ ] `.projects` lists exactly the packages to publish under `# packages`. To stop publishing a
      package, remove it there; `IsPackable` is not what CI reads.
- [ ] Run the tests CI will run:
      ```
      dotnet test Albatross.CommandLine.Test -c Release
      dotnet test Albatross.CommandLine.CodeGen.UnitTest -c Release
      ```
- [ ] Smoke-test `Sample.CommandLine` (build it, then run from `bin/Debug/net10.0`):
  - [ ] `--help` lists every command
  - [ ] `config --compact` prints one line of valid JSON
  - [ ] `test error-handling` prints the `CommandOutput` error envelope on stderr and exits 1
  - [ ] `long-running`, Ctrl+C during the wait: exits 130
  - [ ] `long-running --slow x`, Ctrl+C during the option handler: exits 130 with
        `OptionTaskCancellation` on stderr (not `0xC000013A`)
  - [ ] `test interrutable-output 2000000`, Ctrl+C mid-print: stops promptly, exits 130
- [ ] Inspect the packed core package:
      `dotnet pack Albatross.CommandLine -c Release -o <dir>`, then open the `.nupkg`:
  - [ ] `analyzers/dotnet/cs/` holds only `Albatross.*` assemblies. Any other DLL is redistributed
        third-party code and must be added to `third-party.txt`. (Earlier 9.0.0 builds bundled
        `Humanizer.dll`; current ones do not.)
  - [ ] `LICENSE`, `third-party.txt`, `README.md` and `icon.png` are at the package root.
- [ ] `third-party.txt` lists every third-party source file or binary the packages contain, with
      its license.
- [ ] Docs:
  - [ ] `docfx_project/release-notes.md` has the 9.0.0 entry up to date.
  - [ ] Each package README's **Dependencies** section matches its `.csproj` versions.
  - [ ] `skills/alba-cli/SKILL.md` reflects any public API change.
  - [ ] `docfx build docfx_project/docfx.json` succeeds without warnings for changed articles.
- [ ] Push `main` (this publishes the docs site).

### 2. Publish a release candidate

- [ ] `git switch rc && git merge main && git push origin rc`
- [ ] Note the RC number: `git rev-list --count rc`. The workflow publishes `9.0.0-rc.<that number>`.
- [ ] The GitHub Actions run is green, and the packages appear in GitHub Packages.
- [ ] Install the RC from GitHub Packages into a consumer project and run one command.

### 3. Promote an RC to nuget.org

- [ ] Tag the exact `rc` commit that produced the RC:
      `git tag v9.0.0-rc.N <commit> && git push origin v9.0.0-rc.N`.
- [ ] The packages are listed on nuget.org, and a GitHub prerelease exists for the tag.
- [ ] Update the user-level `alba-cli-upgrade` skill if it pins an RC version (it currently pins
      `9.0.0-rc.284`; the latest tagged RC is `9.0.0-rc.294`).

### 4. Stable 9.0.0

- [ ] Every `.csproj` references only stable packages. A stable package cannot depend on a
      prerelease one. (System.CommandLine is at `3.0.0`; tick once that change is pushed.)
- [ ] Microsoft runtime packages (`Microsoft.Extensions.*`) are on the latest 10.x patch, not 11.x.
- [x] Remove prerelease wording from the release notes, `whats-new-v9.md` and the package READMEs.
      Done 2026-11-24, along with `ai-instructions.md`, `outputs-library.md`, `defaults-library.md`
      and the `alba-cli` skill. These pages go live on the next `main` push.
- [ ] A `production` branch exists, and contains the commit to release.
- [ ] `git tag v9.0.0 <commit> && git push origin v9.0.0`
- [ ] The packages are listed on nuget.org, and a GitHub release exists for the tag.
- [ ] On `main`, bump `<Version>` to the next version. Until that is done, every `rc` push fails,
      because `v9.0.0` now exists.

### Open items before stable 9.0.0

Status as of 2026-11-24: the latest RC is `v9.0.0-rc.294`, tagged on `main` HEAD (`b0c1e05`), and
`main` is in sync with `origin/main`.

- [x] **System.CommandLine v3 GA.** The core references `3.0.0` (uncommitted as of 2026-11-24,
      together with Microsoft.Extensions 10.0.15 and SourceLink 11.0.100); both CI test projects
      pass in Release on SDK 10.0.301.
- [ ] There is no `production` branch yet, locally or on `origin` (needed for section 4).
- [x] Merge `preaction-ctrlc-workaround`: landed on `main` as `971e7a2` (Ctrl+C fix for option
      handlers) and `aa3f3bb` (`third-party.txt`); both are in `rc.294`.
- [x] dotnet/command-line-api#2836: not a blocker, no action planned. The bug ships in
      System.CommandLine 3.0.0 and our `PreActionTerminationHandler` workaround stays. Remove it (and
      its `third-party.txt` entry) only if a later System.CommandLine release fixes the bug. See
      [scommandline-preaction-cancellation-bug.md](scommandline-preaction-cancellation-bug.md).
- [x] `Albatross.CommandLine/README.md` and `docfx_project/index.md` list System.CommandLine 3.0.0
      and Hosting 10.0.15; the index's package table drops CodeGen and adds Outputs.
- [x] `docfx_project/articles/defaults-library.md` and the Defaults README list the current
      dependency versions and the `net10.0` target.
- [x] `Print`'s indented output was invalid JSON when redirected (Spectre wrapped long strings at its
      80-column fallback width). Fixed: when the stream is redirected, `Print` writes plain indented
      JSON directly. Covered by `Extensions_Print.Redirected_LongStringStaysValidJson`.
- [x] Outputs relies on reflection-based `System.Text.Json`, so it fails in Native AOT or trimmed
      apps (including .NET 10 file-based apps, which default to `PublishAot=true`). Documented, and
      the public print methods and the `GlobalErrorHandler` constructor carry
      `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]`; an AOT app gets IL2026/IL3050 at each
      call site (verified). Registering `GlobalErrorHandler` through DI does not warn.
- [x] Decide the status of [cancellable-print-output.tsk.md](completed/cancellable-print-output.tsk.md):
      completed — `PrintAsync` is the interruptible path; `Print` stays colored.
- [x] Push `main` with the Outputs move to System.Text.Json (`ba5c5de`): pushed, and in `rc.294`.

## Conclusion
