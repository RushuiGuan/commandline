# Quick Start Guide

This guide covers the essential attributes and patterns for creating command-line applications with Albatross.CommandLine.

## Core Concepts

Albatross.CommandLine uses three main attributes to define your command structure:

- **`[Verb]`** - Defines a command and its handler
- **`[Option]`** - Defines named options (e.g., `--name`, `-n`)  
- **`[Argument]`** - Defines positional arguments

## The Verb Attribute

The `[Verb]` attribute transforms a class into a command-line command.

### Basic Verb Usage

```csharp
// Command with custom handler
[Verb<HelloWorldCommandAction>("hello", Description = "The HelloWorld command")]
public record class HelloWorldOptions {
    [Option]
    public required string Name { get; init; }
}
```

### Verb Patterns

```csharp
// 1. Using custom command action
[Verb<MyCustomAction>("process", Description = "Process files")]
public record class ProcessOptions { }

// 2. Sub-commands using space-separated names
[Verb<DatabaseBackupAction>("database backup", Description = "Backup database")]
public record class DatabaseBackupOptions { }

[Verb<DatabaseRestoreAction>("database restore", Description = "Restore database")]
public record class DatabaseRestoreOptions { }
```

## The Option Attribute

Options are named parameters that users specify with `--name` or `-n` syntax.

### Basic Option Usage

```csharp
[Verb<DefaultCommandAction<BasicOptions>>("basic")]
public record class BasicOptions {
    // Required option (non-nullable)
    [Option(Description = "User name")]
    public required string Name { get; init; }
    
    // Optional option (nullable)
    [Option(Description = "User email")]
    public string? Email { get; init; }
    
    // Value type option (required by default)
    [Option(Description = "User age")]
    public required int Age { get; init; }
    
    // Boolean flag (optional by default)
    [Option(Description = "Enable verbose output")]
    public bool Verbose { get; init; }
}
```

Usage: `dotnet run -- basic --name "John" --age 25 --verbose`

### Option Aliases

```csharp
[Verb<DefaultCommandAction<AliasOptions>>("test")]
public record class AliasOptions {
    // Multiple aliases: --int-value, -i, --int
    [Option("i", "int", Description = "A required integer option")]
    public required int IntValue { get; init; }

    // Short and long form: --string-value, -s, --str  
    [Option("s", "str", Description = "An optional string value")]
    public string? StringValue { get; init; }
}
```

Usage: `dotnet run -- test -i 42 -s "hello"` or `dotnet run -- test --int 42 --str "hello"`

### Option Requirement Control

```csharp
[Verb<DefaultCommandAction<RequiredOptions>>("required")]
public record class RequiredOptions {
    // Override requirement with Required flag
    [Option(Required = false, Description = "Normally required but made optional")]
    public decimal NumericValue { get; init; }
    
    // Force requirement on normally optional types
    [Option(Required = true, Description = "Collection with required flag")]
    public required int[] IntValues { get; init; }
    
    // Optional collection (default behavior)
    [Option(Description = "Optional collection")]
    public string[] TextValues { get; set; } = [];
    
    // Required boolean flag
    [Option(Required = true, Description = "Required boolean flag")]
    public bool RequiredBoolValue { get; init; }
}
```

### Option Default Values

```csharp
[Verb<DefaultCommandAction<DefaultOptions>>("defaults")]
public record class DefaultOptions {
    // Use property initializer as default value
    [Option(DefaultToInitializer = true, Description = "Port number")]
    public int Port { get; init; } = 8080;
    
    // Use property initializer for complex defaults  
    [Option(DefaultToInitializer = true, Description = "Current date")]
    public DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.Today);
    
    // Default array value
    [Option(Description = "File extensions")]
    public string[] Extensions { get; init; } = [".txt", ".md"];
}
```

## The Argument Attribute

Arguments are positional parameters that don't require option names.

### Basic Argument Usage

```csharp
[Verb<DefaultCommandAction<ArgumentOptions>>("process")]
public record class ArgumentOptions {
    // Required argument (position 0)
    [Argument(Description = "Input file path")]
    public required string InputFile { get; init; }
    
    // Required argument (position 1)  
    [Argument(Description = "Output directory")]
    public required string OutputDir { get; init; }
    
    // Optional argument (position 2)
    [Argument(Description = "Processing mode")]
    public string? Mode { get; init; }
}
```

Usage: `dotnet run -- process "input.txt" "./output" "fast"`

### Argument Order Rules

**Critical**: The order of property declaration determines argument position.

```csharp
[Verb<DefaultCommandAction<OrderedArguments>>("copy")]
public record class OrderedArguments {
    // Position 0: Always required
    [Argument(Description = "Source file")]
    public required string Source { get; init; }
    
    // Position 1: Required  
    [Argument(Description = "Destination file")]
    public required string Destination { get; init; }
    
    // Position 2: Optional - must come after required ones
    [Argument(Description = "Optional backup directory")]
    public string? BackupDir { get; init; }
    
    // ❌ WRONG: Optional argument before required ones will cause issues
    // [Argument] public string? OptionalFirst { get; init; }
    // [Argument] public required string RequiredLater { get; init; }
}
```

### Mixed Arguments and Options

```csharp
[Verb<HelloWorldCommandAction>("hello", Description = "The HelloWorld command")]
public record class HelloWorldOptions {
    // Arguments come first positionally
    [Argument(Description = "First argument (position 0)")]
    public required string Argument1 { get; init; }

    [Argument(Description = "Optional second argument (position 1)")]
    public string? Argument2 { get; init; }

    // Options can be specified anywhere
    [Option(Description = "Required option")]
    public required string Name { get; init; }

    [Option(Description = "Optional value")]  
    public required int Value { get; init; }

    [Option(DefaultToInitializer = true)]
    public DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.Today);
}
```

Usage: `dotnet run -- hello "arg1" "arg2" --name "John" --value 42`

## Complete Example

Here's a comprehensive example showing all three attributes:

```csharp
using Albatross.CommandLine;
using System;
using System.Threading;
using System.Threading.Tasks;

// Define the options class with attributes
[Verb<FileProcessorAction>("process-file", Description = "Process files with various options")]
public record class FileProcessorOptions {
    // Required positional arguments
    [Argument(Description = "Input file to process")]
    public required string InputFile { get; init; }
    
    [Argument(Description = "Output directory")]  
    public required string OutputDir { get; init; }
    
    // Optional positional argument
    [Argument(Description = "Processing template")]
    public string? Template { get; init; }
    
    // Required options
    [Option(Description = "Output format")]
    public required string Format { get; init; }
    
    // Optional options with aliases
    [Option("v", "verbose", Description = "Enable verbose logging")]
    public bool Verbose { get; init; }
    
    [Option("t", "threads", Description = "Number of processing threads")]
    public int ThreadCount { get; init; } = 1;
    
    // Option with default value
    [Option(DefaultToInitializer = true, Description = "Buffer size in KB")]
    public int BufferSize { get; init; } = 1024;
    
    // Collection option
    [Option(Description = "File extensions to include")]
    public string[] Extensions { get; init; } = [".txt", ".md"];
    
    // Override requirement
    [Option(Required = false, Description = "Timeout in seconds (optional despite value type)")]
    public int Timeout { get; init; }
}

// Implement the command action
public class FileProcessorAction : CommandAction<FileProcessorOptions> {
    public FileProcessorAction(FileProcessorOptions options) : base(options) {
    }

    public override async Task<int> Invoke(CancellationToken cancellationToken) {
        await this.Writer.WriteLineAsync($"Processing: {options.InputFile}");
        await this.Writer.WriteLineAsync($"Output: {options.OutputDir}");
        await this.Writer.WriteLineAsync($"Format: {options.Format}");
        
        if (options.Verbose) {
            await this.Writer.WriteLineAsync($"Template: {options.Template ?? "default"}");
            await this.Writer.WriteLineAsync($"Threads: {options.ThreadCount}");
            await this.Writer.WriteLineAsync($"Buffer: {options.BufferSize}KB");
            await this.Writer.WriteLineAsync($"Extensions: {string.Join(", ", options.Extensions)}");
        }
        
        // Process the file here...
        return 0;
    }
}
```

Usage examples:
```bash
# Minimum required parameters
dotnet run -- process-file "input.txt" "./output" --format "json"

# With optional template argument  
dotnet run -- process-file "input.txt" "./output" "my-template" --format "xml"

# With additional options
dotnet run -- process-file "input.txt" "./output" --format "json" --verbose --threads 4

# Using aliases
dotnet run -- process-file "input.txt" "./output" --format "json" -v -t 8
```

## Key Rules and Best Practices

### Attribute Rules

1. **Verb Attribute**: 
   - Command name can contain spaces for sub-commands
   - Always specify a description

2. **Option Attribute**:
   - Non-nullable properties are required by default
   - Nullable properties are optional by default  
   - Use `Required = true/false` to override default behavior
   - Use `DefaultToInitializer = true` with property initializers

3. **Argument Attribute**:
   - Position determined by property declaration order
   - Required arguments must come before optional ones
   - Use nullable types for optional arguments

4. **Properties Without Attributes**:
   - Properties without `[Option]` or `[Argument]` attributes are **ignored**
   - They won't appear as command-line parameters
   - Useful for internal properties, computed values, or dependency injection

### Nullability Guidelines

```csharp
// ✅ Good patterns
public required string RequiredText { get; init; }    // Required
public string? OptionalText { get; init; }            // Optional  
public bool Flag { get; init; }                       // Optional boolean
public int[] Numbers { get; init; } = [];             // Optional collection

// ✅ Override with Required attribute when needed
[Option(Required = true)]
public bool MustBeSpecified { get; init; }            // Required boolean

[Option(Required = false)]  
public int OptionalNumber { get; init; }              // Optional value type

// ✅ Properties without attributes (ignored by code generator)
public string InternalState { get; init; } = "";      // Not a command parameter
public DateTime CreatedAt { get; init; }              // Internal property

// ❌ Avoid these patterns
public required string? RequiredNullable { get; init; } // Confusing
```

### Command Structure

```csharp
// ✅ Good: Clear command hierarchy
[Verb<DatabaseBackupAction>("database backup")]
[Verb<DatabaseRestoreAction>("database restore")]  
[Verb<DatabaseMigrateAction>("database migrate")]

// ✅ Good: Related commands grouped
[Verb<UserCreateAction>("user create")]
[Verb<UserDeleteAction>("user delete")]
[Verb<UserListAction>("user list")]

// ❌ Avoid: Unclear relationships
[Verb<Action1>("cmd1")]
[Verb<Action2>("command-two")]  
[Verb<Action3>("do_something")]
```

## Next Steps
- **[Examples](examples.md)** - More Examples
- **[Conventions](conventions.md)** - Naming and Coding Conventions
- **[Customization](command-customization.md)** - Command Customization
- **[Code Generator](code-generator.md)** - Understanding automatic code generation
- **[Mutually Exclusive Options Set](mutually-exclusive-options-set.md)** - Support for Mutually Exclusive Options Set using Base Classes
- **[Manual Commands](manual-command.md)** - Creating commands without attributes

With these three core attributes, you can create powerful, type-safe command-line applications with minimal boilerplate code!
