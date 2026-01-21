# Logging & Verbosity

Albatross.CommandLine provides built-in logging support through a global `--verbosity` option that controls the minimum log level for your CLI application.

## Overview

Every command automatically inherits a `--verbosity` (or `-v`) option from the root command. This option is:

- **Recursive** - Available on all commands in the hierarchy
- **Optional** - Defaults to `Error` level if not specified
- **Case-insensitive** - Accepts prefix matching (e.g., `-v d` matches `Debug`)

## Verbosity Levels

The verbosity option maps to standard `Microsoft.Extensions.Logging.LogLevel` values:

| Verbosity Value | LogLevel | Description |
|-----------------|----------|-------------|
| `Verbose` | Trace | Most detailed logging, includes all messages |
| `Debug` | Debug | Debugging information for development |
| `Info` | Information | General operational messages |
| `Warning` | Warning | Potentially harmful situations |
| `Error` | Error | Error events (default) |
| `Critical` | Critical | Severe errors causing application failure |
| `None` | None | Disables all logging |

## Command Line Usage

Use the `--verbosity` or `-v` option to control logging output:

```bash
# Full verbosity names
myapp my-command --verbosity Debug
myapp my-command --verbosity Info

# Short form with prefix matching
myapp my-command -v d       # Debug
myapp my-command -v i       # Info
myapp my-command -v v       # Verbose (Trace)
myapp my-command -v w       # Warning
```

## Using Logging in Command Handlers

Inject `ILogger<T>` into your command handler to write log messages:

```csharp
public class MyCommandHandler : BaseHandler<MyCommandParams> {
    private readonly ILogger<MyCommandHandler> logger;

    public MyCommandHandler(
        ILogger<MyCommandHandler> logger,
        ParseResult result,
        MyCommandParams parameters) : base(result, parameters) {
        this.logger = logger;
    }

    public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
        logger.LogTrace("This is a Trace log");
        logger.LogDebug("This is a Debug log");
        logger.LogInformation("This is an Information log");
        logger.LogWarning("This is a Warning log");
        logger.LogError("This is an Error log");
        logger.LogCritical("This is a Critical log");
        return Task.FromResult(0);
    }
}
```

## Configuring Default Verbosity

### Changing the Global Default

By default, the verbosity level is `Error`. To change the global default for all commands, modify the `VerbosityOption.DefaultValueFactory` before parsing:

```csharp
await using var host = new CommandHost("My CLI Application");

// Change the global default to Info level
host.CommandBuilder.VerbosityOption.DefaultValueFactory = _ => VerbosityOption.Info;

host.RegisterServices(RegisterServices)
    .AddCommands()
    .Parse(args, false)
    .WithDefaults()
    .Build();

return await host.InvokeAsync();
```

### Changing the Default for a Specific Command

To override the default verbosity for a specific command without affecting other commands, use the `partial void Initialize()` method in the generated command class:

```csharp
// Parameters class with the Verb attribute
[Verb<MyCommandHandler>("my-command", Description = "Command with custom default logging")]
public record class MyCommandParams {
}

// Partial class to customize the generated command
public partial class MyCommandCommand {
    /// <summary>
    /// Override the global verbosity default for this command only
    /// </summary>
    partial void Initialize() {
        // Create a new VerbosityOption with Debug as the default
        var myOwnVerbosityOption = new VerbosityOption {
            DefaultValueFactory = _ => VerbosityOption.Debug
        };
        this.Add(myOwnVerbosityOption);
    }
}
```

This technique adds a command-specific `VerbosityOption` that takes precedence over the global recursive option.

## Disabling Logging Entirely

If you don't need logging in your CLI application, simply omit the logging setup methods. Calling `Parse()` without `WithDefaults()` or `WithSerilog()` will not configure any logging provider:

```csharp
await using var host = new CommandHost("My CLI Application");

host.RegisterServices(RegisterServices)
    .AddCommands()
    .Parse(args, false)
    // No WithDefaults() or WithSerilog() - logging is not configured
    .Build();

return await host.InvokeAsync();
```

In this case, `ILogger<T>` injections will still work but log messages will not be written anywhere.

## Serilog Integration

When using the `Albatross.CommandLine.Defaults` package, logging is automatically configured with Serilog via the `WithDefaults()` or `WithSerilog()` extension methods:

```csharp
host.Parse(args, false)
    .WithDefaults()  // Configures both Serilog and appsettings.json
    .Build();

// Or configure Serilog only
host.Parse(args, false)
    .WithSerilog()
    .Build();
```

The Serilog configuration:
- Sets the minimum level based on the `--verbosity` option
- Writes to console with error and above going to stderr
- Enriches logs with context information

## How It Works

1. **CommandBuilder** creates a global `VerbosityOption` with `Recursive = true`, making it available to all commands
2. When the command is parsed, the verbosity value is extracted from the parse result
3. **WithSerilog()** reads the verbosity and configures Serilog's minimum level accordingly
4. If `--verbosity None` is specified, logging is completely disabled

## Example Output

Running with different verbosity levels:

```bash
# Default (Error) - only errors and critical messages shown
$ myapp test-logging
[ERR] This is an Error log
[FTL] This is a Critical log

# With Debug level
$ myapp test-logging -v d
[DBG] This is a Debug log
[INF] This is an Information log
[WRN] This is a Warning log
[ERR] This is an Error log
[FTL] This is a Critical log

# With Verbose (Trace) level
$ myapp test-logging -v v
[VRB] This is a Trace log
[DBG] This is a Debug log
[INF] This is an Information log
[WRN] This is a Warning log
[ERR] This is an Error log
[FTL] This is a Critical log
```
