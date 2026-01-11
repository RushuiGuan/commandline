# AI Agent Instructions for Albatross.CommandLine

This document provides condensed guidance for AI agents working with the Albatross.CommandLine library.

## Overview

**Albatross.CommandLine** is a .NET library that simplifies CLI application development by wrapping `System.CommandLine` with automatic code generation and dependency injection. It uses Roslyn incremental source generators to create command infrastructure from annotated classes.

**Requirements:** .NET 8+, System.CommandLine 2.0.1+, Microsoft.Extensions.Hosting 8.0.1+

## Core Architecture

### Key Components

| Component | Purpose |
|-----------|---------|
| `CommandHost` | Main orchestrator - manages DI, parsing, and execution |
| `BaseHandler<T>` | Abstract base class for command handlers |
| `VerbAttribute` | Tags parameter classes and links them to handlers |
| `OptionAttribute` / `ArgumentAttribute` | Marks properties as CLI parameters |
| `CommandContext` | Execution context for sharing state between handlers |

### Namespaces

- `Albatross.CommandLine` - Core runtime classes
- `Albatross.CommandLine.Annotations` - All attributes
- `Albatross.CommandLine.CodeGen` - Source generators
- `Albatross.CommandLine.Inputs` - Built-in reusable parameter types
- `Albatross.CommandLine.Defaults` - Pre-configured extensions (Serilog, JSON config)

## Standard Patterns

### Entry Point

```csharp
await using var host = new CommandHost("AppName")
    .RegisterServices(RegisterServices)
    .AddCommands()
    .Parse(args)
    .Build();
return await host.InvokeAsync();

static void RegisterServices(ParseResult result, IServiceCollection services) {
    services.RegisterCommands(); // Generated method
    // Add custom services
}
```

### Command Definition

```csharp
[Verb<MyHandler>("command-name", Description = "Help text")]
public class MyParams {
    [Option(Description = "Output file path")]
    public string? OutputFile { get; init; }

    [Argument(Description = "Input file")]
    public required string InputFile { get; init; }
}

public class MyHandler : BaseHandler<MyParams> {
    public MyHandler(ParseResult result, MyParams parameters) : base(result, parameters) { }

    public override async Task<int> InvokeAsync(CancellationToken token) {
        Writer.WriteLine($"Processing {parameters.InputFile}");
        return 0;
    }
}
```

### Parent Commands (Grouping)

```csharp
[Verb("parent", Description = "Parent command group")]
public class ParentParams { }

[Verb<ChildHandler>("parent child", Description = "Child command")]
public class ChildParams { }
```

## Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Parameters class | `*Params` suffix | `BackupParams` |
| Generated command | Remove `Params`, add `Command` | `BackupCommand` |
| Handler class | `*Handler` or `*CommandHandler` | `BackupHandler` |
| Properties to CLI | kebab-case | `OutputFolder` → `--output-folder` |
| Option aliases | Single char: `-x`, multi: `--name` | `-o`, `--output` |

## Attribute Quick Reference

### VerbAttribute

```csharp
[Verb<THandler>("name")]                    // Standard command
[Verb<TParams, THandler>("name")]           // For base class inheritance
[Verb("name")]                              // Parent command (no handler)
```

### OptionAttribute

```csharp
[Option(Description = "...", Required = true, Aliases = new[] { "-o" })]
[Option(DefaultToInitializer = true)]       // Use property initializer as default
```

### ArgumentAttribute

```csharp
[Argument(Description = "...", Required = true)]
[Argument(ArityMin = 1, ArityMax = 10)]     // For collections
```

### Default Requirement Rules

- Non-nullable types → Required
- Nullable types → Optional
- Boolean flags → Optional
- Collections → Optional
- With `DefaultToInitializer = true` → Optional

## Advanced Patterns

### Reusable Parameters

```csharp
// Define once
public class InputFileOption : Option<FileInfo> {
    public InputFileOption() : base("--input", "Input file path") {
        AddAlias("-i");
        AddValidator(result => {
            var file = result.GetValueOrDefault<FileInfo>();
            if (file != null && !file.Exists)
                result.ErrorMessage = $"File not found: {file.FullName}";
        });
    }
}

[Verb<MyHandler>("process")]
public class ProcessParams {
    // Use via attribute
    [UseOption<InputFileOption>]
    public FileInfo? Input { get; init; }
}
```

### Option Preprocessing (Validation)

```csharp
[DefaultNameAliases("--my-option", "-m")]
[OptionHandler<MyOption, MyOptionHandler>]
public class MyOption : Option<string> {
    public MyOption(string name, params string[] aliases): base(name, aliases){ 
        Description = "Custom option description";
    }
}

public class MyOptionHandler : IAsyncOptionHandler<string?> {
    private readonly ICommandContext _context;

    public MyOptionHandler(ICommandContext context) => _context = context;

    public async Task HandleAsync(MyOption symbol, ParseResult result, CancellationToken token) {
        var text = result.GetValue(symbol);
        if(!string.IsNullOrEmpty(text)) {
            var err = await ValidateAsync(text);
            if(!string.IsNullOrEmpty(err)) {
                _context.SetInputActionStatus(new OptionHandlerStatus(symbol.Name, false, err));
            }
        }
    }
    async Task<string?> ValidateAsync(string value) {
        // custom validation logic
    }
}
```

### Input Transformation

Transform a simple input (e.g., string identifier) into a complex object (e.g., DTO) before it reaches the command handler.

```csharp
// Define the output type
public record class InstrumentSummary {
    public required int Id { get; init; }
    public required string Name { get; init; }
}

// Define the option with transformation handler
[DefaultNameAliases("--instrument", "-i")]
[OptionHandler<InstrumentOption, InstrumentOptionHandler, InstrumentSummary>]
public class InstrumentOption : Option<string> {
    public InstrumentOption(string name, params string[] alias) : base(name, alias) {
        this.Description = "The security instrument identifier";
    }
}

// Implement the transformation handler
public class InstrumentOptionHandler : IAsyncOptionHandler<InstrumentOption, InstrumentSummary> {
    private readonly InstrumentService instrumentService;

    public InstrumentOptionHandler(InstrumentService instrumentService) {
        this.instrumentService = instrumentService;
    }

    public async Task<OptionHandlerResult<InstrumentSummary>> InvokeAsync(
        InstrumentOption option, ParseResult result, CancellationToken cancellationToken) {
        var text = result.GetValue(option);
        if (string.IsNullOrEmpty(text)) {
            return new OptionHandlerResult<InstrumentSummary>();
        } else {
            var data = await instrumentService.GetSummary(text, cancellationToken);
            return new OptionHandlerResult<InstrumentSummary>(data);
        }
    }
}

// Use in params class - property type is the transformed type
[Verb<GetInstrumentDetails>("instrument detail", Description = "Get instrument details")]
public record class GetInstrumentDetailsParams {
    [UseOption<InstrumentOption>]
    public required InstrumentSummary Summary { get; init; }
}

// Handler receives the transformed object
public class GetInstrumentDetails : BaseHandler<GetInstrumentDetailsParams> {
    public GetInstrumentDetails(ParseResult result, GetInstrumentDetailsParams parameters)
        : base(result, parameters) { }

    public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
        Writer.WriteLine($"Instrument: {parameters.Summary.Name} (ID: {parameters.Summary.Id})");
        return Task.FromResult(0);
    }
}
```

### Mutually Exclusive Parameters

```csharp
public abstract class BaseParams {
    [Option]
    public bool Verbose { get; init; }
}

[Verb<SharedHandler>("cmd optionA")]
public class OptionAParams : BaseParams {
    [Option]
    public string? OptionA { get; init; }
}

[Verb<SharedHandler>("cmd optionB")]
public class OptionBParams : BaseParams {
    [Option]
    public int OptionB { get; init; }
}

public class SharedHandler : BaseHandler<BaseParams> {
    public override async Task<int> InvokeAsync(CancellationToken token) {
        return parameters switch {
            OptionAParams a => HandleA(a),
            OptionBParams b => HandleB(b),
            _ => 1
        };
    }
}
```

### Command Customization

Generated command classes are partial. Add custom initialization:

```csharp
// In your code
public partial class MyCommand {
    partial void Initialize() {
        // Access Option_* and Argument_* properties
        Option_OutputFile.AddValidator(result => {
            // Custom validation
        });
    }
}
```

## Built-in Input Types

From `Albatross.CommandLine.Inputs`:

- `InputFileOption` / `InputFileArgument` - Validates file exists
- `InputDirectoryOption` / `InputDirectoryArgument` - Validates directory exists
- `OutputFileOption` - For output file paths
- `OutputDirectoryOption` / `OutputDirectoryArgument` - For output directories
- `OutputDirectoryWithAutoCreateOption` - Auto-creates if missing
- `FormatExpressionOption` - For format expression inputs

## Execution Pipeline

```
Parse Arguments
    ↓
Create DI Scope
    ↓
Option PreActions (IAsyncOptionHandler implementations)
    ↓ (short-circuit if failed)
Command Action (IAsyncCommandHandler.InvokeAsync)
    ↓
Dispose Scope
```

## Code Generation Output

The source generator creates:

1. **Command classes** - One per `[Verb]` with `Option_*` and `Argument_*` properties
2. **`RegisterCommands()`** - Registers params classes and handlers in DI
3. **`AddCommands()`** - Adds commands to CommandBuilder

View generated code: Set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in project file, check `obj/generated/`.

## Common Tasks

### Add a New Command

1. Create params class with `[Verb<Handler>("name")]`
2. Add properties with `[Option]` or `[Argument]`
3. Create handler extending `BaseHandler<T>`
4. Implement `InvokeAsync`

## DI Services Available

- `ParseResult` - System.CommandLine parse result
- `ICommandContext` - Execution context
- `T parameters` - The params class instance
- `ILogger<T>` - Logging
- Any custom registered services

## Key Differences from Raw System.CommandLine

1. **Attribute-driven** - No manual command building
2. **DI-first** - All handlers receive dependencies via constructor
3. **Async-only** - Handlers use `InvokeAsync` with cancellation support
4. **Generated code** - Commands, registration, and wiring are auto-generated
5. **Parameters injection** - Handler receives typed params object, not individual values
