---
name: alba-cli
description: >
  How to create command-line applications using the Albatross.CommandLine library packages.
  Use this skill whenever the user is working with Albatross.CommandLine, creating a CLI app,
  adding commands, defining verbs, options, arguments, or handlers in a .NET command-line project —
  even if they don't explicitly say "Albatross". Trigger on requests like "add a new command",
  "create a CLI app", "add an option to this command", "set up a command handler", "bootstrap a
  Program.cs", "add logging to my CLI", "create a subcommand", or anything related to building
  CLI tools with this library. Also trigger when the user references CommandHost, [Verb], [Option],
  [Argument], BaseHandler, IAsyncCommandHandler, RegisterCommands, AddCommands, or WithDefaults.
---

# Albatross.CommandLine — CLI Development Guide

This skill documents how to correctly build .NET CLI applications using the Albatross.CommandLine
family of packages. Follow these patterns precisely — the library uses Roslyn source generators,
so the conventions are load-bearing.

**Full documentation:** https://rushuiguan.github.io/commandline/

---

## Packages

| Package | Purpose | When to use |
|---------|---------|-------------|
| `Albatross.CommandLine` | Core runtime: `CommandHost`, attributes, `BaseHandler<T>` | Always |
| `Albatross.CommandLine.CodeGen` | Roslyn source generator (generates `AddCommands`, `RegisterCommands`) | Always (auto-referenced with core) |
| `Albatross.CommandLine.Defaults` | Pre-configured Serilog + JSON config via `WithDefaults()` | New standalone CLI apps |
| `Albatross.CommandLine.Inputs` | Pre-built validated `Option`/`Argument` types for files and directories | When you need file/directory inputs |
| `Albatross.CommandLine.CodeAnalysis` | Roslyn analyzers for best practices | Optional |

Install:
```
dotnet new console -n MyCliApp --framework net10.0
dotnet add package Albatross.CommandLine
dotnet add package Albatross.CommandLine.Defaults   # optional
dotnet add package Albatross.CommandLine.Inputs     # optional
```

Always use `--framework net10.0` when creating new CLI projects.

---

## Bootstrap (Program.cs)

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Defaults; // if using Defaults

internal class Program {
    static async Task<int> Main(string[] args) {
        await using var host = new CommandHost("My App Name")
            .RegisterServices(RegisterServices)
            .AddCommands()      // generated — won't compile until first [Verb] exists
            .Parse(args)
            .WithDefaults()     // optional: adds Serilog + JSON config (must come after Parse)
            .Build();
        return await host.InvokeAsync();
    }

    static void RegisterServices(ParseResult result, IServiceCollection services) {
        services.RegisterCommands(); // generated — registers all handlers and params classes
        // Register your own services:
        services.AddSingleton<IMyService, MyService>();
    }
}
```

**Order matters:** `RegisterServices` → `AddCommands` → `Parse` → `WithDefaults` → `Build`

`WithDefaults()` (and `WithConfig()` / `WithSerilog()`) must come **after** `Parse()` because they
read the `--verbosity` option from the parse result.

---

## Critical Requirements

**All params and handler classes must be in a namespace.** The source generator fails with
`Invalid namespace identifier: ''` if files use top-level declarations without a namespace.
Always wrap classes in `namespace YourApp { ... }`.

**`ParseResult` is in `System.CommandLine`**, not `System.CommandLine.Parsing`:
```csharp
using System.CommandLine; // correct
```

---

## Defining a Command

Every command needs two things: a **params class** (decorated with `[Verb<THandler>]`) and a
**handler class** (extends `BaseHandler<T>`). The source generator wires them together.

### 1. Params Class

```csharp
using Albatross.CommandLine.Annotations;

namespace MyApp {
    [Verb<HelloWorldHandler>("hello", Description = "Say hello to someone")]
    public record class HelloWorldParams {
        [Argument(Description = "The name to greet")]
        public required string Name { get; init; }

        // DefaultToInitializer = true is required to use the property initializer as a default.
        // Without it, non-nullable value types (int, bool, etc.) are treated as required.
        [Option(DefaultToInitializer = true, Description = "Number of times to repeat")]
        public int Count { get; init; } = 1;

        [Option("--shout", "-s", Description = "Use uppercase")]
        public bool Shout { get; init; }
    }
}
```

### 2. Handler Class

```csharp
using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using System.CommandLine;

namespace MyApp {
    public class HelloWorldHandler : BaseHandler<HelloWorldParams> {
        private readonly ILogger<HelloWorldHandler> logger;

        public HelloWorldHandler(
            ParseResult result,
            HelloWorldParams parameters,
            ILogger<HelloWorldHandler> logger) : base(result, parameters) {
            this.logger = logger;
        }

        public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
            var greeting = parameters.Shout
                ? $"HELLO, {parameters.Name.ToUpper()}!"
                : $"Hello, {parameters.Name}!";

            for (int i = 0; i < parameters.Count; i++) {
                Writer.WriteLine(greeting); // Writer writes to stdout
            }
            logger.LogInformation("Greeted {Name}", parameters.Name);
            return 0; // exit code
        }
    }
}
```

`BaseHandler<T>` gives you:
- `parameters` — the typed params instance
- `result` — the raw `ParseResult`
- `Writer` — a `TextWriter` for stdout (useful for testability)

---

## Arguments vs Options

| | Argument | Option |
|--|--|--|
| Syntax | Positional (no name) | Named (`--flag value`) |
| Order | Matters (declaration order) | Any order |
| Attribute | `[Argument]` | `[Option]` |
| Required by default | `required` keyword | `required` keyword |

**Required/optional rules:**
- Non-nullable + `required` keyword → required
- Nullable (`string?`, `int?`) → optional
- Boolean → optional (flag, defaults false)
- Collection → optional
- `[Option(DefaultToInitializer = true)]` with property initializer → optional with default

---

## Subcommand Hierarchy

Use spaces in the verb name to create parent→child relationships:

```csharp
// Parent command (no handler needed)
[Verb("config", Description = "Configuration commands")]
public record class ConfigParams { }

// Children
[Verb<ConfigGetHandler>("config get", Description = "Get a config value")]
public record class ConfigGetParams {
    [Argument]
    public required string Key { get; init; }
}

[Verb<ConfigSetHandler>("config set", Description = "Set a config value")]
public record class ConfigSetParams {
    [Argument]
    public required string Key { get; init; }

    [Argument]
    public required string Value { get; init; }
}
```

Usage: `myapp config get SomeKey`, `myapp config set SomeKey somevalue`

---

## Assembly-Level Verbs

When you don't want a dedicated params class (e.g., for using built-in handler types), declare
the verb at assembly level:

```csharp
// In Verbs.cs or GlobalUsings.cs
[assembly: Verb<MyParamsClass, MyHandler>("command-name", Description = "...")]
```

This is especially useful with `Albatross.EFCore.Admin` handlers — see the alba-efcore skill.

---

## Naming Conventions

| Element | Pattern | Example |
|---------|---------|---------|
| Params class | `*Params` suffix | `BackupParams` |
| Generated command class | Remove `Params`, add `Command` | `BackupCommand` |
| Handler class | `*Handler` or `*CommandHandler` | `BackupHandler` |
| Properties → CLI names | kebab-case | `OutputFolder` → `--output-folder` |

The generator derives option and argument names from property names automatically.

---

## Defaults Package (Logging & Config)

Add `Albatross.CommandLine.Defaults` for pre-wired Serilog + JSON configuration:

```csharp
// Combined (recommended for new apps)
.Parse(args)
.WithDefaults()
.Build()

// Or separately:
.Parse(args)
.WithConfig()    // loads appsettings.json + environment-specific files
.WithSerilog()   // configures Serilog with --verbosity support
.Build()
```

**What `WithConfig()` does:**
- Loads `appsettings.json` (optional, won't fail if missing)
- Loads `appsettings.{DOTNET_ENVIRONMENT}.json` for env-specific settings
- Registers `IConfiguration`, `EnvironmentSetting`, `ProgramSetting`, `IHostEnvironment`

**What `WithSerilog()` does:**
- Configures Serilog as logging provider
- Reads `--verbosity` option (Trace/Debug/Information/Warning/Error/Critical/None)
- Loads `serilog.json` if present
- Routes error+critical to stderr, others to stdout

**Inject `IConfiguration` in a handler:**

```csharp
public class MyHandler : BaseHandler<MyParams> {
    private readonly IConfiguration config;

    public MyHandler(ParseResult result, MyParams parameters, IConfiguration config)
        : base(result, parameters) {
        this.config = config;
    }

    public override Task<int> InvokeAsync(CancellationToken token) {
        var connStr = config.GetConnectionString("Default");
        return Task.FromResult(0);
    }
}
```

> **Note:** `Albatross.CommandLine.Defaults` uses the latest package versions (not .NET Standard
> 2.1). For strict version compatibility, configure logging/config manually instead.

---

## Reusable Option and Argument Classes

Define once, use across many commands. The class must follow the required constructor signature.

### Reusable Option

```csharp
[DefaultNameAliases("--input-dir", "--in", "-i")]
public class InputDirectoryOption : Option<DirectoryInfo> {
    // Constructor signature is required by the source generator
    public InputDirectoryOption(string name, params string[] aliases) : base(name, aliases) {
        Description = "Specify an existing input directory";
        AddValidator(result => {
            if (result.GetValueForOption(this) is DirectoryInfo dir && !dir.Exists)
                result.ErrorMessage = $"Directory {dir.FullName} does not exist";
        });
    }
}
```

### Reusable Argument

```csharp
public class InputDirectoryArgument : Argument<DirectoryInfo> {
    // Constructor signature is required by the source generator
    public InputDirectoryArgument(string name) : base(name) {
        Description = "Specify an existing input directory";
        AddValidator(result => {
            if (result.GetValueForArgument(this) is DirectoryInfo dir && !dir.Exists)
                result.ErrorMessage = $"Directory {dir.FullName} does not exist";
        });
    }
}
```

### Using Reusable Parameters

```csharp
[Verb<BackupHandler>("backup")]
public record class BackupParams {
    // Uses default names from [DefaultNameAliases] → --input-dir, --in, -i
    [UseOption<InputDirectoryOption>]
    public required DirectoryInfo Source { get; init; }

    // UseCustomName = true → derives name from property → --destination
    [UseOption<InputDirectoryOption>(UseCustomName = true)]
    public required DirectoryInfo Destination { get; init; }

    [UseArgument<InputDirectoryArgument>]
    public required DirectoryInfo ExtraDir { get; init; }
}
```

---

## Built-in Inputs (Albatross.CommandLine.Inputs)

Pre-built reusable types — no need to write your own for common file/directory scenarios:

| Type | What it does |
|------|-------------|
| `InputFileOption` / `InputFileArgument` | Validates file exists |
| `InputDirectoryOption` / `InputDirectoryArgument` | Validates directory exists |
| `OutputFileOption` | For output file paths |
| `OutputDirectoryOption` / `OutputDirectoryArgument` | For output directory paths |
| `OutputDirectoryWithAutoCreateOption` | Auto-creates directory if missing |
| `FormatExpressionOption` | For format string inputs |
| `TimeZoneOption` | For timezone name inputs |

```csharp
[Verb<ProcessHandler>("process")]
public record class ProcessParams {
    [UseOption<InputFileOption>]
    public required FileInfo InputFile { get; init; }

    [UseOption<OutputDirectoryWithAutoCreateOption>]
    public required DirectoryInfo OutputDir { get; init; }
}
```

---

## Option Pre-processing and Validation (OptionHandler)

Run async validation (DB calls, API checks) before the command handler executes.
Attach an `[OptionHandler]` to a reusable option class to short-circuit on failure:

```csharp
[DefaultNameAliases("--api-key", "-k")]
[OptionHandler<ApiKeyOption, ApiKeyValidator>]
public class ApiKeyOption : Option<string> {
    public ApiKeyOption(string name, params string[] aliases) : base(name, aliases) {
        Description = "The API key";
    }
}

public class ApiKeyValidator : IAsyncOptionHandler<ApiKeyOption> {
    private readonly IAuthService auth;
    private readonly ICommandContext context;

    public ApiKeyValidator(IAuthService auth, ICommandContext context) {
        this.auth = auth;
        this.context = context;
    }

    public async Task InvokeAsync(ApiKeyOption option, ParseResult result,
        CancellationToken cancellationToken) {
        var key = result.GetValue(option);
        if (!await auth.IsValidAsync(key, cancellationToken)) {
            // Short-circuit: command handler will not run
            context.SetInputActionStatus(new OptionHandlerStatus(option.Name, false, "Invalid API key"));
        }
    }
}
```

---

## Input Transformation (OptionHandler with output type)

Transform a simple CLI input (e.g., string ID) into a rich object before the handler runs:

```csharp
[DefaultNameAliases("--instrument", "-i")]
[OptionHandler<InstrumentOption, InstrumentOptionHandler, InstrumentSummary>]
public class InstrumentOption : Option<string> {
    public InstrumentOption(string name, params string[] aliases) : base(name, aliases) {
        Description = "Instrument identifier";
    }
}

public class InstrumentOptionHandler : IAsyncOptionHandler<InstrumentOption, InstrumentSummary> {
    private readonly InstrumentService svc;
    public InstrumentOptionHandler(InstrumentService svc) { this.svc = svc; }

    public async Task<OptionHandlerResult<InstrumentSummary>> InvokeAsync(
        InstrumentOption option, ParseResult result, CancellationToken ct) {
        var id = result.GetValue(option);
        if (string.IsNullOrEmpty(id)) return new OptionHandlerResult<InstrumentSummary>();
        var summary = await svc.GetSummaryAsync(id, ct);
        return new OptionHandlerResult<InstrumentSummary>(summary);
    }
}

// Property type is the *output* type, not the raw CLI type
[Verb<PriceHandler>("get-price")]
public record class GetPriceParams {
    [UseOption<InstrumentOption>]
    public required InstrumentSummary Instrument { get; init; }
}
```

---

## Mutually Exclusive Parameter Sets

When multiple commands share logic but have different options, use a shared base handler:

```csharp
public abstract record class CodeGenParams {
    [Option]
    public required FileInfo ProjectFile { get; init; }
}

[Verb<CodeGenHandler>("codegen csharp")]
public record class CSharpCodeGenParams : CodeGenParams {
    [Option]
    public required string Namespace { get; init; }
}

[Verb<CodeGenHandler>("codegen typescript")]
public record class TypeScriptCodeGenParams : CodeGenParams {
    [Option]
    public bool StrictMode { get; init; }
}

public class CodeGenHandler : BaseHandler<CodeGenParams> {
    public override async Task<int> InvokeAsync(CancellationToken ct) {
        return parameters switch {
            CSharpCodeGenParams cs => await GenerateCSharp(cs, ct),
            TypeScriptCodeGenParams ts => await GenerateTypeScript(ts, ct),
            _ => 1
        };
    }
}
```

---

## Command Customization

Generated command classes are `partial`. Use the `Initialize()` partial method for custom setup:

```csharp
public partial class MyCommand {
    partial void Initialize() {
        // Access generated Option_* and Argument_* properties
        Option_OutputFile.AddValidator(result => {
            // Custom validation logic
        });
        Argument_Name.SetDefaultValue("World");
    }
}
```

---

## Execution Pipeline

```
Parse CLI Arguments
      ↓
Create DI Scope
      ↓
Option PreActions (IAsyncOptionHandler — validation/transformation, in declaration order)
      ↓ (short-circuit if any SetInputActionStatus with failure)
Command Action (IAsyncCommandHandler.InvokeAsync)
      ↓
Dispose DI Scope
```

Option handlers run before the command handler. If any option handler marks the context as failed,
the command handler is skipped.

---

## DI Services Available in Handlers

All of these can be injected into any handler constructor:

| Service | Description |
|---------|-------------|
| `ParseResult` | Raw parse result from System.CommandLine |
| `T parameters` | The typed params class instance |
| `ICommandContext` | For sharing state / short-circuiting between option handlers |
| `ILogger<T>` | Type-safe Serilog/MEL logging |
| `IConfiguration` | App configuration (requires `WithConfig()` or `WithDefaults()`) |
| Any custom service | Anything registered in `RegisterServices` |

---

## Viewing Generated Code

To inspect what the source generator produces, add to your `.csproj`:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files appear in `obj/generated/`. The generator creates:
- `{CommandName}Command.g.cs` — partial command class with `Option_*` and `Argument_*` properties
- `CodeGenExtensions.g.cs` — `RegisterCommands()` and `AddCommands()` methods

---

## Quick Checklist: Adding a New Command

1. Create a params class with `[Verb<THandler>("verb-name")]`
2. Add properties with `[Option]` or `[Argument]`
3. Create handler class extending `BaseHandler<TParams>`
4. Implement `override Task<int> InvokeAsync(CancellationToken ct)`
5. Build — the source generator regenerates `RegisterCommands()` and `AddCommands()` automatically
6. Test: `dotnet run -- verb-name [args] [options]`
