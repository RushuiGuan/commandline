# Conventions

This document outlines the naming and coding conventions used in Albatross.CommandLine to ensure consistency and predictability.

## Options Class Naming Convention

Options classes should be created with a postfix of `Options`. For example, either `BackupCommandOptions` or `BackupOptions` is acceptable. When annotated with the `[VerbAttribute]`, the code generator will create the command with the name of `BackupCommand`. Notice that it removes the `Options` postfix and appends `Command` if it is not already there.

```csharp
[Verb<BackupCommandAction>("backup")]
public class BackupCommandOptions {
    // ...
}
// Generated command class
public sealed partial class BackupCommand : Command {
}
```

## Command Class Naming Convention

Generated command classes follow this pattern:
- Remove the `Options` suffix from the options class name
- Append `Command` suffix if not already present
- Handle naming conflicts by appending incremental numbers (should be avoided)

## Command Handler Naming Convention

There is no strict requirement on the naming of command handlers. Common patterns include:
- Postfixing with `CommandAction` (e.g., `BackupCommandAction`)
- Postfixing with `CommandHandler` (e.g., `BackupCommandHandler`)
- Simple names matching the command (e.g., `Backup`)

## Property Nullability Convention

The nullability of properties determines whether options and arguments are required by default:

```csharp
public record class ExampleOptions {
    // Required - non-nullable reference type
    public required string Name { get; init; }
    
    // Required - non-nullable value type
    public required int Value { get; init; }
    
    // Optional - nullable reference type
    public string? Description { get; init; }
    
    // Optional - nullable value type
    public int? OptionalValue { get; init; }
    
    // Override default behavior with Required attribute
    [Option(Required = false)]
    public int NonRequiredValue { get; init; }
    
    [Option(Required = true)]
    public string? RequiredNullableValue { get; init; }
}
```

## Generated Option Names Convention

By default, option names are the lowercase kebab-cased property name. Property `FileName` becomes `--file-name`. 
```csharp
public record class BackupOptions {
    // This option has multiple names: --file-name, -f, --file
    [Option("f", "file", Description = "The name of the file to backup")]
    public string FileName { get; set; } = string.Empty;
}
```

## Alias Prefix Convention

Aliases are automatically prefixed according to these rules:
- Single character aliases get single dash prefix: `"f"` → `"-f"`
- Multi-character aliases get double dash prefix: `"file"` → `"--file"`
- No prefix is added if dashes are already present: `"--file-name"` remains `"--file-name"`

## Generated Property Names Convention

Within the command class, option and argument properties are generated with specific prefixes:
- Option properties: `Option_PropertyName` (e.g., `Option_FileName`)
- Argument properties: `Argument_PropertyName` (e.g., `Argument_Name`)

```csharp
public sealed partial class BackupCommand : Command {
    public Option<string> Option_FileName { get; }
    public Argument<string> Argument_Source { get; }
}
```

## Verb Attribute Usage Patterns

The `[VerbAttribute]` supports multiple usage patterns:

```csharp
// Basic usage - no handler specified (uses HelpCommandAction)
[Verb("command-name")]
public record class CommandOptions { }

// With generic handler type
[Verb<CommandHandler>("command-name")]
public record class CommandOptions { }

// Assembly-level usage for external options/handlers
[assembly: Verb<OptionsClass, HandlerClass>("command-name")]

// Multiple verbs on same class for sub-commands
[Verb<Handler>("parent child1")]
[Verb<Handler>("parent child2")]
public record class ParentChildOptions { }
```

## Command Key Convention

Command keys (the string parameter in `[Verb]`) define the command hierarchy:
- Single word: `"backup"` creates a root command
- Space-separated: `"project create"` creates a sub-command structure
- Parent commands are automatically created if not explicitly defined

## Default Values Convention

Use the `DefaultToInitializer = true` attribute property to use property initializers as default values:

```csharp
public record class CommandOptions {
    [Option(DefaultToInitializer = true)]
    public int Count { get; init; } = 10;
    
    [Option(DefaultToInitializer = true)]
    public DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.Today);
}
```

## Argument Order Convention

Arguments must be declared in the order they will be consumed:
- Required arguments first
- Optional (nullable) arguments last
- Use the `Order` property to explicitly control ordering if needed

```csharp
public record class CommandOptions {
    [Argument(Description = "First required argument")]
    public required string First { get; init; }
    
    [Argument(Description = "Second required argument")]
    public required int Second { get; init; }
    
    [Argument(Description = "Optional argument goes last")]
    public string? Third { get; init; }
}
```

## Command Customization Convention

Customize generated commands by implementing partial methods or extending the generated partial class:

```csharp
public partial class MyCommand {
    partial void Initialize() {
        this.Option_Description.Validators.Add(r => {
            var text = r.GetRequiredValue(this.Option_Description);
            if (text.Length < 3) {
                r.AddError("Description must be at least 3 characters long.");
            }
        });
    }
}
```

## Hidden Parameters Convention

Use the `Hidden = true` property to create parameters that don't appear in help text but are still available for use:

```csharp
public record class CommandOptions {
    [Option(Hidden = true)]
    public string? InternalOption { get; init; }
    
    [Argument(Hidden = true)]
    public string? DebugArgument { get; init; }
}
```

