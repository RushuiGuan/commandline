# Migrating System.CommandLine from 2.0.0-beta4 to 2.0.2

This guide covers breaking changes when upgrading from `System.CommandLine` version `2.0.0-beta4.22272.1` to `2.0.2`. The stable release introduced significant API changes, particularly around validators, handlers, and symbol configuration.

## Validator Changes

### AddValidator → Validators.Add

The `AddValidator` method was replaced with a mutable `Validators` collection property.

```csharp
// Beta4
command.AddValidator(validator);
option.AddValidator(validator);
argument.AddValidator(validator);

// 2.0.2
command.Validators.Add(validator);
option.Validators.Add(validator);
argument.Validators.Add(validator);
```

### ErrorMessage Property → AddError Method

The `SymbolResult.ErrorMessage` property was converted to an `AddError()` method to support multiple errors per symbol.

```csharp
// Beta4
option.AddValidator(result =>
{
    if (result.GetValueForOption(option) < 1)
    {
        result.ErrorMessage = "Must be greater than 0";
    }
});

// 2.0.2
option.Validators.Add(result =>
{
    if (result.GetValue(option) < 1)
    {
        result.AddError("Must be greater than 0");
    }
});
```

### Complete Validator Example

```csharp
// Beta4
public partial class MyCommand {
    partial void Initialize() {
        this.Option_Count.AddValidator(r => {
            var value = r.GetValueForOption(this.Option_Count);
            if (value < 0) {
                r.ErrorMessage = "Count cannot be negative";
            }
        });
    }
}

// 2.0.2
public partial class MyCommand {
    partial void Initialize() {
        this.Option_Count.Validators.Add(r => {
            var value = r.GetValue(this.Option_Count);
            if (value < 0) {
                r.AddError("Count cannot be negative");
            }
        });
    }
}
```

### Command Validators for Mutually Exclusive Options

```csharp
// Beta4
command.AddValidator(commandResult =>
{
    var hasOne = commandResult.Children.Any(sr => sr is OptionResult or && or.Option.Name == "--one");
    var hasTwo = commandResult.Children.Any(sr => sr is OptionResult or && or.Option.Name == "--two");
    if (hasOne && hasTwo)
    {
        commandResult.ErrorMessage = "Options '--one' and '--two' cannot be used together.";
    }
});

// 2.0.2
command.Validators.Add(commandResult =>
{
    var hasOne = commandResult.Children.Any(sr => sr is OptionResult or && or.Option.Name == "--one");
    var hasTwo = commandResult.Children.Any(sr => sr is OptionResult or && or.Option.Name == "--two");
    if (hasOne && hasTwo)
    {
        commandResult.AddError("Options '--one' and '--two' cannot be used together.");
    }
});
```

## Handler Changes

### SetHandler → SetAction

The `SetHandler` method was replaced with `SetAction`, and `ICommandHandler` was replaced with `CommandLineAction`.

```csharp
// Beta4
rootCommand.SetHandler((InvocationContext context) =>
{
    string? value = context.ParseResult.GetValueForOption(option);
});

// 2.0.2
rootCommand.SetAction((ParseResult parseResult) =>
{
    string? value = parseResult.GetValue(option);
});
```

### Async Handlers

`CancellationToken` is now a mandatory parameter for async actions:

```csharp
// Beta4
rootCommand.SetHandler(async (InvocationContext context) =>
{
    var token = context.GetCancellationToken();
    await DoWorkAsync(token);
});

// 2.0.2
rootCommand.SetAction(async (ParseResult parseResult, CancellationToken token) =>
{
    await DoWorkAsync(token);
});
```

### InvocationContext Removed

`InvocationContext` was eliminated. `ParseResult` and `CancellationToken` are now passed directly:

```csharp
// Beta4
rootCommand.SetHandler((InvocationContext context) =>
{
    var parseResult = context.ParseResult;
    var token = context.GetCancellationToken();
    var value = parseResult.GetValueForOption(myOption);
});

// 2.0.2
rootCommand.SetAction((ParseResult parseResult, CancellationToken token) =>
{
    var value = parseResult.GetValue(myOption);
});
```

## Option and Argument Changes

### Mandatory Name Parameter

Constructors now require the name parameter:

```csharp
// Beta4
Option<int> option = new();
option.Name = "--number";

// 2.0.2
Option<int> option = new("--number");
```

### Aliases in Constructor

Aliases can now be specified directly in the constructor:

```csharp
// Beta4
Option<bool> option = new("--help", "An option with aliases.");
option.Aliases.Add("-h");
option.Aliases.Add("/h");

// 2.0.2
Option<bool> option = new("--help", "-h", "/h")
{
    Description = "An option with aliases."
};
```

### SetDefaultValue → DefaultValueFactory

```csharp
// Beta4
Option<int> option = new("--number");
option.SetDefaultValue(42);

// 2.0.2
Option<int> option = new("--number")
{
    DefaultValueFactory = _ => 42
};
```

### Custom Parsing with CustomParser

```csharp
// Beta4
Argument<Uri> uri = new("arg", parse: result =>
{
    if (!Uri.TryCreate(result.Tokens.Single().Value, UriKind.RelativeOrAbsolute, out var uriValue))
    {
        result.ErrorMessage = "Invalid URI format.";
        return null;
    }
    return uriValue;
});

// 2.0.2
Argument<Uri> uri = new("arg")
{
    CustomParser = result =>
    {
        if (!Uri.TryCreate(result.Tokens.Single().Value, UriKind.RelativeOrAbsolute, out var uriValue))
        {
            result.AddError("Invalid URI format.");
            return null;
        }
        return uriValue;
    }
};
```

## ParseResult Changes

### GetValueForOption → GetValue

```csharp
// Beta4
var value = parseResult.GetValueForOption(option);
var argValue = parseResult.GetValueForArgument(argument);

// 2.0.2
var value = parseResult.GetValue(option);
var argValue = parseResult.GetValue(argument);
```

### FindResultFor → GetResult

```csharp
// Beta4
var optionResult = parseResult.FindResultFor(option);

// 2.0.2
var optionResult = parseResult.GetResult(option);
```

### Getting Values by Name

```csharp
// 2.0.2
int number = parseResult.GetValue<int>("--number");
```

## Migration Checklist

| Beta4 | 2.0.2 |
|-------|-------|
| `symbol.AddValidator(delegate)` | `symbol.Validators.Add(delegate)` |
| `result.ErrorMessage = "msg"` | `result.AddError("msg")` |
| `result.GetValueForOption(opt)` | `result.GetValue(opt)` |
| `result.GetValueForArgument(arg)` | `result.GetValue(arg)` |
| `parseResult.FindResultFor(symbol)` | `parseResult.GetResult(symbol)` |
| `command.SetHandler(action)` | `command.SetAction(action)` |
| `command.Handler` | `command.Action` |
| `InvocationContext context` | `ParseResult parseResult, CancellationToken token` |
| `context.ParseResult` | `parseResult` (direct parameter) |
| `context.GetCancellationToken()` | `token` (direct parameter) |
| `option.SetDefaultValue(val)` | `option.DefaultValueFactory = _ => val` |
| `new Option<T>()` then set Name | `new Option<T>("--name")` |

## References

- [Microsoft Migration Guide](https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5)
- [System.CommandLine Validation Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/how-to-customize-parsing-and-validation)
- [GitHub Releases](https://github.com/dotnet/command-line-api/releases)
