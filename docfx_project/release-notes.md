# Release Notes

## 8.0.5 - TrackerOption & Automatic Resource Disposal

### New Features

- **`TrackerOption`** - New reusable option in `Albatross.CommandLine.Inputs` for resumable long-running jobs:
  - Tracks processed items in a file, automatically skipping already-completed work on restart
  - Immediate write-through persistence (survives process kill, power failure)
  - Thread-safe for concurrent batch processing
  - Case-insensitive by default, with `CaseSensitiveTrackerOption` variant
  - Proper cancellation handling (`OperationCanceledException` propagates, other errors are logged)
  ```csharp
  [UseOption<TrackerOption>]
  public Tracker? Tracker { get; init; }

  // In handler:
  await tracker.ProcessIfNew(itemId, (ct) => ProcessItem(ct), logger, cancellationToken);
  ```

- **Automatic Resource Disposal** - `CommandContext` now implements `IAsyncDisposable` and automatically disposes values stored via option handlers:
  - Values implementing `IAsyncDisposable` are disposed asynchronously
  - Values implementing `IDisposable` are disposed synchronously
  - No manual cleanup required for resources created by option handlers

### Documentation

- Added comprehensive [TrackerOption](articles/reusable-parameters/tracker-file-option.md) guide
- Updated [Command Context](articles/command-context.md) with automatic disposal documentation

---

## 8.0.4 - Customizable Verbosity & New Inputs

### New Features

- **Customizable Default Verbosity** - The global `VerbosityOption` now supports changing the default verbosity level:
  ```csharp
  // Change global default
  CommandBuilder.VerbosityOption.DefaultValueFactory = _ => VerbosityOption.Info;
  ```
- **Per-Command Verbosity Override** - Individual commands can override the default verbosity using `partial void Initialize()` to add their own `VerbosityOption`
- **New `TimeZoneOption`** - Added a reusable option in `Albatross.CommandLine.Inputs` for time zone input with validation (supports both Windows and IANA formats)
- **Disable Logging Option** - New `Parse(args, false)` overload allows disabling logging entirely when not needed

### Improvements

- **Reduced Log Noise** - Internal framework logging changed from `Information` to `Debug` level for cleaner output
- **New Extension Method** - Added `GetVerbosityOption()` extension on `ParseResult` to retrieve the active verbosity option

### Documentation

- Added comprehensive [Logging & Verbosity](articles/logging-verbosity.md) guide
- Added [System.CommandLine Migration](articles/system-commandline-migration.md) guide for migrating from beta4 to 2.0.2

---

## 8.0.3 - Bug Fix & Documentation

- **Bug Fix** - Changed `MaxArity` for collection arguments from `int.MaxValue` to `100_000` to align with the `MaximumArity` constant defined in System.CommandLine 2.0.2
- **Dependency Update** - Updated `System.CommandLine` to version 2.0.2
- **Packaging** - Disabled symbol package generation for `Albatross.CommandLine.CodeGen` (analyzer packages do not support symbol packages)
- **Documentation** - Added comprehensive XML documentation comments to all public APIs across the library

---

## 8.0.2 - Documentation & Packaging

- **Documentation** - Published comprehensive documentation site with quick start guide, core concepts, migration instructions, and AI agent guidance
- **SourceLink** - Added `Microsoft.SourceLink.GitHub` for source-stepping debugging support
- **Symbol packages** - Now publishing `.snupkg` symbol packages to NuGet
- **Package metadata** - Added package tags for improved discoverability, release notes URL, and documentation link

---

## 8.0.1 - Major Rewrite

Version 8 is a complete redesign of Albatross.CommandLine, driven by the stable release of `System.CommandLine 2.0.1` (previously 2.0.0-beta4). The upstream library introduced significant breaking changes that required rethinking our approach.

### Why a Rewrite?

- **System.CommandLine 2.0.1** removed `InvocationContext` and changed how commands, options, and handlers interact
- This created an opportunity to simplify the library's architecture and improve the developer experience
- The new design is more idiomatic, leverages modern C# features, and provides better async/cancellation support

### Key Architectural Changes

| Area | v7 | v8 |
|------|----|----|
| Handler execution | Sync `Invoke(InvocationContext)` | Async `InvokeAsync(CancellationToken)` |
| Parameter injection | `IOptions<T>` wrapper | Direct `T` injection |
| Handler specification | Constructor parameter | Generic type argument `[Verb<THandler>]` |
| Attributes namespace | `Albatross.CommandLine` | `Albatross.CommandLine.Annotations` |
| Property annotation | Implicit (all properties are options) | Explicit (must use `[Option]` or `[Argument]`) |
| Entry point | Custom `Setup` class | Generic `CommandHost` |
| Class naming | `*Options` suffix | `*Params` suffix |

### New Capabilities

- **Reusable Parameters** - Create custom `Option<T>` and `Argument<T>` classes with `[UseOption<T>]` and `[UseArgument<T>]`
- **Option Preprocessing** - Async validation with dependency injection via `IAsyncOptionHandler<T>`
- **Input Transformation** - Transform raw input into complex objects before reaching the handler
- **Partial Command Classes** - Customize generated commands via `partial void Initialize()`
- **Built-in Input Types** - Ready-to-use file/directory validators in `Albatross.CommandLine.Inputs`

### Migration

This is a breaking release. See the [Migration Guide](articles/ai-migration-instructions.md) for step-by-step upgrade instructions.

---

## 7.8.7
* Change the property type of ArgumentAttribute.ArityMin and ArgumentAttribute.ArityMax from int? to int because Nullable<int> is not a valid Attribute property type
## 7.8.5
* `VerbAttribute` can now target an assembly.  When doing so, a command will be generated if its `OptionsClass` property is populated.
* `albatross.commandline.codegen.debug.txt` will no longer be generated by default.  To turn it on, create the following entries in the project file:
    ```xml
    <PropertyGroup>
        <EmitAlbatrossCodeGenDebugFile>true</EmitAlbatrossCodeGenDebugFile>
    </PropertyGroup>
    <ItemGroup>
        <CompilerVisibleProperty Include="EmitAlbatrossCodeGenDebugFile" />
    </ItemGroup>
    ```
## 7.8.3
* Bug fix on the `OptionAttribute.Required` property.  It is now working as expected for required collection and boolean types.

## 7.8.1 - Add Argument Support
* ^ Incorrect version for this release.  Should have been a minor instead of patch release 
* Add support for arguments.
* Remove `OptionAttribute.Ignore` property and replace its functionality with a new attribute class `Albatross.CommandLine.IgnoreAttribute`
## 7.8.0 - Add and implement the logic for the `OptionAttribute.DefaultToInitializer` Property
* If the `OptionAttribute.DefaultToInitializer` property is set to `true`, the code generator will generate a default value using the initializer value of the property.
## 7.7.0 - Behavior Adjustment
* If the `VerbAttribute` is created without the handler type parameter, it will default to use `HelpCommandHandler`instead of `DefaultCommandHandler`.
* References `Spectre.Console` version 0.49.1 and Add extension methods for Spectre at `SpectreExtensions`.
* Rename `OptionAttribute.Skip` property to `OptionAttribute.Ignore` property.
## 7.6.0 - Sub Command Support + Upgrades in Different Areas
* The `VerbAttribute` can now be created without the handler type parameter.  The system will use the `DefaultCommandHandler`.
* New sub command support.  See Sub Commands.
* Create `HelpCommandHandler` that will display the help message of a command
* If the RootCommand is invoke without any command, it will display the help message without the error message - `Required command was not provided.`.  Same behavior applies to any other parent commands.
* If a command has its own handler, it will no longer be overwritten with the `GlobalCommandHandler`.  This gives developers more flexibility in creating custom commands.
* Add help messages to the global options.