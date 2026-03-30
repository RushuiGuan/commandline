# Albatross.CommandLine Skill

This skill helps you build CLI applications using the Albatross.CommandLine library.

## Usage

```
/albatross-commandline <action> [arguments]
```

### Available Actions

| Action | Description |
|--------|-------------|
| `new-command <name> [description]` | Create a new command with handler |
| `new-project <name>` | Bootstrap a new CLI project |
| `add-reusable-option <name>` | Create a reusable option type |

---

# Library Overview

**Albatross.CommandLine** is a .NET library that simplifies CLI development by wrapping `System.CommandLine` with automatic code generation and dependency injection.

**Requirements:** .NET 8+, System.CommandLine 2.0.1+

**Key Components:**
- `CommandHost` - Main orchestrator for DI, parsing, and execution
- `BaseHandler<T>` - Abstract base class for command handlers
- `[Verb<THandler>]` - Attribute to define commands
- `[Option]` / `[Argument]` - Attributes for CLI parameters

---

# Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Parameters class | `*Params` suffix | `BackupParams` |
| Generated command | Remove `Params`, add `Command` | `BackupCommand` |
| Handler class | `*Handler` suffix | `BackupHandler` |
| Properties to CLI | kebab-case | `OutputFolder` -> `--output-folder` |

---

# Action: new-command

Create a new command with its handler.

## Template

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace {{Namespace}} {
    [Verb<{{HandlerName}}>("{{command-name}}", Description = "{{description}}")]
    public record class {{ParamsName}} {
        // Add [Option] for named parameters (--option-name value)
        // Add [Argument] for positional parameters
    }

    public class {{HandlerName}} : BaseHandler<{{ParamsName}}> {
        public {{HandlerName}}(ParseResult result, {{ParamsName}} parameters)
            : base(result, parameters) { }

        public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
            await Writer.WriteLineAsync("Command executed");
            return 0; // 0 = success, non-zero = error
        }
    }
}
```

## Options (Named Parameters)

```csharp
// Required (non-nullable)
[Option(Description = "Output file path")]
public required string OutputFile { get; init; }

// Optional (nullable)
[Option(Description = "Config path")]
public string? ConfigFile { get; init; }

// With alias
[Option("-o", "--out", Description = "Output")]
public required string Output { get; init; }

// Boolean flag (always optional)
[Option(Description = "Verbose output")]
public bool Verbose { get; init; }

// With default value
[Option(DefaultToInitializer = true, Description = "Max retries")]
public int MaxRetries { get; init; } = 3;
```

## Arguments (Positional Parameters)

```csharp
// Required (order matters - declaration order = position)
[Argument(Description = "Source file")]
public required string Source { get; init; }

// Optional (must come after required)
[Argument(Description = "Destination")]
public string? Destination { get; init; }

// Collection
[Argument(Description = "Input files")]
public string[] Files { get; init; } = [];
```

## Subcommands

Use spaces in verb name for hierarchies:

```csharp
// Parent (no handler)
[Verb("config", Description = "Configuration commands")]
public record class ConfigParams { }

// Children
[Verb<ConfigSetHandler>("config set", Description = "Set config")]
public record class ConfigSetParams { ... }

[Verb<ConfigGetHandler>("config get", Description = "Get config")]
public record class ConfigGetParams { ... }
```

## Dependency Injection

```csharp
public class MyHandler : BaseHandler<MyParams> {
    private readonly IMyService _service;

    public MyHandler(ParseResult result, MyParams parameters, IMyService service)
        : base(result, parameters) {
        _service = service;
    }
}
```

## Custom Validation

```csharp
public partial class MyCommand {
    partial void Initialize() {
        Option_OutputFile.Validators.Add(result => {
            var value = result.GetValue(Option_OutputFile);
            if (string.IsNullOrEmpty(value))
                result.AddError("Output file required");
        });
    }
}
```

## Example

For `new-command backup "Backup files"`:

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp {
    [Verb<BackupHandler>("backup", Description = "Backup files")]
    public record class BackupParams {
        [Argument(Description = "Source directory")]
        public required DirectoryInfo Source { get; init; }

        [Argument(Description = "Destination directory")]
        public required DirectoryInfo Destination { get; init; }

        [Option("-p", "--pattern", Description = "File pattern")]
        public string Pattern { get; init; } = "*.*";

        [Option(Description = "Overwrite existing")]
        public bool Overwrite { get; init; }
    }

    public class BackupHandler : BaseHandler<BackupParams> {
        public BackupHandler(ParseResult result, BackupParams parameters)
            : base(result, parameters) { }

        public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
            var files = parameters.Source.GetFiles(parameters.Pattern);
            foreach (var file in files) {
                var dest = Path.Combine(parameters.Destination.FullName, file.Name);
                file.CopyTo(dest, parameters.Overwrite);
            }
            await Writer.WriteLineAsync($"Backed up {files.Length} files");
            return 0;
        }
    }
}
```

---

# Action: new-project

Bootstrap a new CLI project using Albatross.CommandLine.

## Steps

1. Create a new console project
2. Add NuGet packages
3. Create Program.cs with CommandHost bootstrap
4. Create a sample command

## Template

### Project file (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Albatross.CommandLine" Version="*" />
    <PackageReference Include="Albatross.CommandLine.Defaults" Version="*" />
  </ItemGroup>

  <!-- Optional: view generated code -->
  <PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  </PropertyGroup>
</Project>
```

### Program.cs

```csharp
using System.Threading.Tasks;
using Albatross.CommandLine;
using Albatross.CommandLine.Defaults;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace {{ProjectName}} {
    internal class Program {
        static async Task<int> Main(string[] args) {
            await using var host = new CommandHost("{{ProjectName}}")
                .RegisterServices(RegisterServices)
                .AddCommands()
                .Parse(args)
                .WithDefaults()
                .Build();
            return await host.InvokeAsync();
        }

        static void RegisterServices(ParseResult result, IServiceCollection services) {
            services.RegisterCommands();
            // Register your services here
        }
    }
}
```

### Sample Command (HelloWorld.cs)

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace {{ProjectName}} {
    [Verb<HelloHandler>("hello", Description = "Say hello")]
    public record class HelloParams {
        [Argument(Description = "Name to greet")]
        public required string Name { get; init; }
    }

    public class HelloHandler : BaseHandler<HelloParams> {
        public HelloHandler(ParseResult result, HelloParams parameters)
            : base(result, parameters) { }

        public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
            await Writer.WriteLineAsync($"Hello, {parameters.Name}!");
            return 0;
        }
    }
}
```

## CLI Commands

```bash
# Create project
dotnet new console -n {{ProjectName}}
cd {{ProjectName}}

# Add packages
dotnet add package Albatross.CommandLine
dotnet add package Albatross.CommandLine.Defaults

# Build and run
dotnet build
dotnet run -- hello World
```

---

# Action: add-reusable-option

Create a reusable option type with validation that can be shared across commands.

## Template

```csharp
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace {{Namespace}} {
    [DefaultNameAliases("--{{option-name}}", "-{{alias}}")]
    public class {{OptionName}}Option : Option<{{Type}}> {
        public {{OptionName}}Option(string name, params string[] aliases)
            : base(name, aliases) {
            Description = "{{description}}";
            this.AddValidator(result => {
                var value = result.GetValueForOption(this);
                // Add validation logic here
            });
        }
    }
}
```

## Usage in Command

```csharp
[Verb<MyHandler>("mycommand")]
public record class MyParams {
    [UseOption<{{OptionName}}Option>]
    public required {{Type}} {{PropertyName}} { get; init; }

    // Use custom name instead of default
    [UseOption<{{OptionName}}Option>(UseCustomName = true)]
    public required {{Type}} Another{{PropertyName}} { get; init; }
}
```

## Example: InputFileOption

```csharp
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;

namespace MyApp {
    [DefaultNameAliases("--input", "-i")]
    public class InputFileOption : Option<FileInfo> {
        public InputFileOption(string name, params string[] aliases)
            : base(name, aliases) {
            Description = "Input file path";
            this.AddValidator(result => {
                var file = result.GetValueForOption(this);
                if (file != null && !file.Exists)
                    result.ErrorMessage = $"File not found: {file.FullName}";
            });
        }
    }
}
```

## Reusable Argument

```csharp
public class InputFileArgument : Argument<FileInfo> {
    public InputFileArgument(string name) : base(name) {
        Description = "Input file path";
        this.AddValidator(result => {
            var file = result.GetValueForArgument(this);
            if (file != null && !file.Exists)
                result.ErrorMessage = $"File not found: {file.FullName}";
        });
    }
}

// Usage
[Verb<ProcessHandler>("process")]
public record class ProcessParams {
    [UseArgument<InputFileArgument>]
    public required FileInfo Input { get; init; }
}
```

## Built-in Types (Albatross.CommandLine.Inputs)

Available when you add `Albatross.CommandLine.Inputs` package:

- `InputFileOption` / `InputFileArgument` - Validates file exists
- `InputDirectoryOption` / `InputDirectoryArgument` - Validates directory exists
- `OutputFileOption` - For output file paths
- `OutputDirectoryOption` / `OutputDirectoryArgument` - For output directories
- `OutputDirectoryWithAutoCreateOption` - Auto-creates if missing

---

# Workflow

1. **Parse the action** from user input
2. **Ask clarifying questions** based on action:
   - `new-command`: command name, description, options/arguments needed
   - `new-project`: project name, optional features
   - `add-reusable-option`: option name, type, validation rules
3. **Generate the code** following templates above
4. **Remind user** that `AddCommands()` and `RegisterCommands()` are auto-generated

---

# Quick Reference

## Default Requirement Rules

- Non-nullable types -> Required
- Nullable types -> Optional
- Boolean flags -> Optional
- Collections -> Optional
- `DefaultToInitializer = true` -> Optional

## Execution Pipeline

```
Parse Arguments -> Option PreActions -> Command Action -> Dispose
```

## DI Services Available

- `ParseResult` - Parse result from System.CommandLine
- `ICommandContext` - Execution context
- `T parameters` - The params class instance
- `ILogger<T>` - Logging
- Any registered services
