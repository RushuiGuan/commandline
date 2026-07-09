# Logging & Verbosity

Starting in v9, `Albatross.CommandLine` keeps **standard output and standard error clean by default**. The core library does not add a logging provider, does not emit log lines, and no longer creates a global `--verbosity` option. A command owns its output; logging is an explicit, opt-in concern.

> [!IMPORTANT]
> **Changed in v9.** v8 added a recursive `--verbosity`/`-v` option to every command and logged to the console by default. That behavior has been removed. If you relied on it, see [Migrating from v8](#migrating-from-v8-verbosity) below.

## The v9 Model

- **Clean by default** — with no logging configured, nothing is written to `stdout`/`stderr` except what your command writes. This makes output safe to pipe, parse, and feed to tooling.
- **Opt in with Defaults** — the [`Albatross.CommandLine.Defaults`](defaults-library.md) package adds Serilog logging via `WithDefaults()` / `WithSerilog()`.
- **Logging goes to a file, not the console** — Serilog writes to a daily-rolling file under `IApplicationPath.LogRoot`. No console sink is attached, so log lines never mix with command output.
- **Verbosity is configuration, not a CLI flag** — the log level is controlled by a `Serilog` section in `appsettings.json`, editable at deploy time without recompiling.

## Enabling Logging

Add the `Albatross.CommandLine.Defaults` package and call `WithDefaults()`. Because logs are written to `IApplicationPath.LogRoot`, register an `IApplicationPath` and pass its `ConfigRoot` so configuration is read from the same stable location:

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Defaults;
using Albatross.Config;
using Microsoft.Extensions.DependencyInjection;

static async Task<int> Main(string[] args) {
    var appPath = new ApplicationPath(false, ["myapp"], "myapp", null, args);
    appPath.Init();

    await using var host = new CommandHost("My CLI App")
        .RegisterServices((result, services) => {
            services.AddSingleton<IApplicationPath>(appPath);
            services.RegisterCommands();
        })
        .AddCommands()
        .Parse(args)
        .WithDefaults(appPath.ConfigRoot)   // config + file logging
        .Build();
    return await host.InvokeAsync();
}
```

## Using Logging in Command Handlers

Inject `ILogger<T>` into your handler to write log messages:

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

With the default configuration, messages at `Information` and above are written to the log file; `Trace` and `Debug` are filtered out until you raise the level (see below).

## Controlling Verbosity

The log level is the `Serilog:MinimumLevel` setting. Because `WithConfig()` loads `appsettings.json` from the directory you pass to `WithDefaults()` (typically `ConfigRoot`), you can change verbosity by editing that file — no rebuild required:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

- `Default` sets the global minimum level (`Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`).
- `Override` sets per-namespace floors so framework noise can be suppressed independently of your own code.

Serilog levels map to `Microsoft.Extensions.Logging.LogLevel` as follows:

| Serilog Level | LogLevel |
|---------------|----------|
| Verbose | Trace |
| Debug | Debug |
| Information | Information |
| Warning | Warning |
| Error | Error |
| Fatal | Critical |

### Per-Environment Verbosity

`WithConfig()` also loads `appsettings.{DOTNET_ENVIRONMENT}.json`. Put a lower level in `appsettings.Development.json` and a higher one in `appsettings.Production.json`, and the level follows the environment automatically.

## Where Logs Go

`WithSerilog()` writes to:

```
{IApplicationPath.LogRoot}/{entryAssemblyName}-.log
```

with a daily rolling interval, so the actual file is e.g. `myapp-20260708.log`. `LogRoot` is resolved from the `IApplicationPath` you register; its location depends on the `ApplicationPath` configuration (user vs. system paths). If no `IApplicationPath` is registered, `WithSerilog()` throws an `InvalidOperationException` explaining how to register one.

### Zero-Config Baseline

Even with no `Serilog` section present, logging works. `WithSerilog()` sets these defaults in code, then layers any configuration on top:

- Minimum level `Information`
- `Enrich.FromLogContext()` and `Enrich.WithThreadId()`
- The file sink, using the default output template:

```
{Timestamp:yyyy-MM-dd HH:mm:sszzz} {SourceContext} {ThreadId} [{Level:w3}] {Message:lj}{NewLine}{Exception}
```

The file sink and its `LogRoot`-derived path are always added in code, so configuration never needs to know the log path.

## Adding Sinks or Enrichers

Because `ReadFrom.Configuration` is layered on top of the code baseline, the `Serilog` section can add anything Serilog's configuration supports — additional sinks, enrichers, or filters:

```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "Enrich": [ "WithMachineName" ]
  }
}
```

Reference the corresponding Serilog package (for example `Serilog.Enrichers.Environment` for `WithMachineName`) so the setting can be resolved.

> [!NOTE]
> A sink whose secret is supplied through an environment variable (e.g. `Serilog__WriteTo__Slack__Args__…`) still needs its `Name` element defined in a JSON file. `ReadFrom.Configuration` fails fast at startup if a `WriteTo` entry has no `Name`.

## Disabling Logging Entirely

Logging is off unless you opt in. Simply omit `WithDefaults()` / `WithSerilog()`:

```csharp
await using var host = new CommandHost("My CLI Application")
    .RegisterServices(RegisterServices)
    .AddCommands()
    .Parse(args)
    // No WithDefaults()/WithSerilog() — no logging provider is configured
    .Build();

return await host.InvokeAsync();
```

`ILogger<T>` injections still resolve, but messages are not written anywhere.

## Migrating from v8 Verbosity

| v8 | v9 |
|----|----|
| Recursive `--verbosity`/`-v` option added automatically | Removed — no global logging flag |
| Default console logging at `Error` | No logging by default; opt in with `WithDefaults()` |
| Logs written to the console | Logs written to a file under `IApplicationPath.LogRoot` |
| Change level with `-v Debug` at the command line | Set `Serilog:MinimumLevel` in `appsettings.json` |
| `CommandBuilder.VerbosityOption.DefaultValueFactory` | Set `Serilog:MinimumLevel:Default` in configuration |

## Adding Your Own Recursive Option

The library no longer imposes a `--verbosity` flag, but nothing stops you from adding one — or any other cross-cutting option. `CommandBuilder.RootCommand` is public, so add a recursive option to it **before** `Parse()` and read the value afterward:

```csharp
var host = new CommandHost("My CLI App");

// A recursive option is inherited by every command in the hierarchy.
var verbosity = new Option<LogLevel>("--verbosity", "-v") {
    Recursive = true,
    DefaultValueFactory = _ => LogLevel.Information,
};
host.CommandBuilder.RootCommand.Options.Add(verbosity);

host.RegisterServices(RegisterServices)
    .AddCommands()
    .Parse(args)                                   // must come after the option is added
    .WithConfig(appPath.ConfigRoot)
    .Build();
```

You can then read the parsed value and wire it into your own logging setup. This is the same mechanism the library itself uses; there is no dedicated API to learn.
