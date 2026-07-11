# Albatross.CommandLine.Defaults

The `Albatross.CommandLine.Defaults` package provides pre-configured integrations for configuration management and Serilog logging. It eliminates boilerplate setup by providing sensible, opinionated defaults for most command-line scenarios.

## Design Philosophy

### The Core Library: Maximum Compatibility

The main `Albatross.CommandLine` library is designed with **broad compatibility** as its primary goal. It targets .NET Standard 2.1 and keeps its dependency surface small:

```xml
<!-- Albatross.CommandLine dependencies -->
<PackageReference Include="System.CommandLine" Version="3.0.0-preview.5.26302.115" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
```

This approach ensures that:
- Applications targeting .NET 8 or newer can consume the library
- Version conflicts with other dependencies in your project are minimized
- The library integrates smoothly into existing projects with established dependency trees

> [!NOTE]
> Because v9 references the **System.CommandLine v3 prerelease**, `Albatross.CommandLine` v9 is published on the prerelease channel until System.CommandLine v3 reaches GA. The v8.x line remains the current stable release for consumers who cannot take a prerelease dependency.

### The Defaults Library: Opinionated Integrations

In contrast, `Albatross.CommandLine.Defaults` is **opinionated**. It targets `net8.0` and pulls in the heavier integrations (Serilog, `Albatross.Config`) that the core deliberately leaves out, so the core stays dependency-light while applications that want batteries-included setup get it with one call.

**The trade-off is intentional**: if you have strict dependency version requirements, or want a different logging framework, use the core `Albatross.CommandLine` library directly and configure services yourself. If you are building a CLI application and want sensible defaults, use `Albatross.CommandLine.Defaults`.

## What the Library Provides

The Defaults library provides three entry points:

1. **Configuration Management** via `WithConfig()`
2. **File-Based Serilog Logging** via `WithSerilog()`
3. **Combined Setup** via `WithDefaults()` (calls both)

A defining v9 behavior: **logging goes to a file, never the console.** The console (`stdout`/`stderr`) is reserved for the command's own output. See [Logging & Verbosity](logging-verbosity.md) for the full model.

## Quick Start

`WithSerilog()` writes logs to `IApplicationPath.LogRoot`. Registering an `IApplicationPath` before the host is built lets you control that location; if none is registered, `WithSerilog()` falls back to a `DefaultApplicationPath` (logging to a `log` folder under the application base directory) so logging works with no extra setup. The typical pattern still creates the `ApplicationPath` up front, registers it, and passes its `ConfigRoot` to `WithDefaults()`:

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Defaults;
using Albatross.Config;
using Microsoft.Extensions.DependencyInjection;

static async Task<int> Main(string[] args) {
    var appPath = new ApplicationPath(false, ["myapp"], "myapp", null, args);
    appPath.Init();   // creates ConfigRoot / LogRoot / DataRoot

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

## Extension Methods

### WithDefaults()

`WithDefaults()` is a convenience method that applies both configuration and Serilog logging:

```csharp
public static CommandHost WithDefaults(this CommandHost commandHost, string? configDirectory = null)
    => commandHost.WithConfig(configDirectory).WithSerilog();
```

The optional `configDirectory` is forwarded to `WithConfig()`. Passing `appPath.ConfigRoot` makes configuration load from the stable, writable config directory rather than the application's `bin` folder. This is the recommended approach for most applications.

### WithConfig()

The `WithConfig()` method sets up JSON-based configuration with environment support:

```csharp
host.Parse(args)
    .WithConfig(appPath.ConfigRoot)
    .Build();
```

**What it configures:**

1. **JSON Configuration Files**
   - Loads `appsettings.json` from `configDirectory` (or the application base directory when omitted)
   - Loads an environment-specific file such as `appsettings.Development.json` or `appsettings.Production.json`
   - Configuration files are optional (loading won't fail if they are missing)

2. **Environment Variables**
   - Adds the environment variable configuration provider; environment variables can override JSON settings

3. **Environment Detection**
   - Uses the `DOTNET_ENVIRONMENT` environment variable to choose the environment-specific config file (`Development`, `Staging`, `Production`, …)

4. **Registered Services**
   - `EnvironmentSetting` — the current environment name
   - `ProgramSetting` — wraps the `IConfiguration` instance
   - `IHostEnvironment` — the standard host environment abstraction

> [!NOTE]
> `WithConfig(configDirectory)` **replaces** the base path — it does not layer the `bin` folder and `configDirectory` together. When you pass `appPath.ConfigRoot`, configuration is read solely from `ConfigRoot`.

**Example appsettings.json:**

```json
{
  "ConnectionStrings": {
    "Database": "Server=localhost;Database=MyApp"
  },
  "AppSettings": {
    "MaxRetries": 3,
    "Timeout": "00:00:30"
  }
}
```

**Using configuration in a handler:**

```csharp
public class MyHandler : BaseHandler<MyParams> {
    private readonly IConfiguration configuration;

    public MyHandler(
        IConfiguration configuration,
        ParseResult result,
        MyParams parameters) : base(result, parameters) {
        this.configuration = configuration;
    }

    public override Task<int> InvokeAsync(CancellationToken token) {
        var connectionString = configuration.GetConnectionString("Database");
        var maxRetries = configuration.GetValue<int>("AppSettings:MaxRetries");
        // ...
    }
}
```

### WithSerilog()

`WithSerilog()` configures Serilog as the logging provider, writing to a **file** — there is no console sink.

```csharp
host.Parse(args)
    .WithSerilog()
    .Build();
```

**What it configures:**

1. **File Sink under `IApplicationPath.LogRoot`**
   - Resolves `IApplicationPath` from the service container. If none is registered, it falls back to a `DefaultApplicationPath`, whose `LogRoot` is a `log` folder under the application base directory — logging works with zero setup.
   - Writes a daily-rolling file named `{entryAssemblyName}-.log` (Serilog inserts the date, e.g. `myapp-20260708.log`).
   - Uses the `DefaultOutputTemplate` constant for formatting.

2. **Code Baseline (works with zero configuration)**
   - Minimum level `Information`
   - `Enrich.FromLogContext()` and `Enrich.WithThreadId()`

3. **Configuration Overrides**
   - Layers `ReadFrom.Configuration(...)` on top of the baseline. A `Serilog` section in `appsettings.json` (loaded by `WithConfig()`) can raise or lower the level, add per-namespace `MinimumLevel:Override`s, and add enrichers or sinks — all editable at deploy time without recompiling.

Because the baseline is applied first and `ReadFrom.Configuration` second, a `Serilog` section overrides **only** the keys it specifies; an absent or empty section leaves the code baseline intact.

> [!NOTE]
> A sink whose secret is supplied through an environment variable (e.g. `Serilog__WriteTo__Slack__Args__…`) still needs its `Name` element defined in a JSON file. `ReadFrom.Configuration` fails fast at startup if a `WriteTo` entry has no `Name`.

## Order of Operations

The extension methods must be called after `Parse()`:

```csharp
await using var host = new CommandHost("My App")
    .RegisterServices(RegisterServices)  // 1. Register your services (incl. IApplicationPath)
    .AddCommands()                        // 2. Add generated commands
    .Parse(args)                          // 3. Parse command line
    .WithDefaults(appPath.ConfigRoot)     // 4. Configure defaults (after parse)
    .Build();                             // 5. Build the host
```

Configuration and logging are wired into the host builder, so they must be in place before `Build()`.

## Using Components Separately

If you only need one of the integrations:

```csharp
// Configuration only (no logging)
host.Parse(args)
    .WithConfig(appPath.ConfigRoot)
    .Build();

// Logging only (no configuration)
host.Parse(args)
    .WithSerilog()
    .Build();
```

## Dependencies

The Defaults package brings in these libraries:

| Package | Version | Purpose |
|---------|---------|---------|
| [Albatross.Config](https://github.com/RushuiGuan/config/tree/main/Albatross.Config) | 8.0.0-rc.51 | Application paths (`IApplicationPath`), configuration, environment handling |
| Serilog | 4.3.0 | Structured logging core |
| Serilog.Extensions.Hosting | 8.0.0 | `UseSerilog` host integration |
| Serilog.Settings.Configuration | 8.0.4 | `ReadFrom.Configuration` support |
| Serilog.Sinks.File | 6.0.0 | Rolling-file sink |
| Serilog.Enrichers.Thread | 4.0.0 | `WithThreadId` enricher |
| Microsoft.Extensions.Hosting | 8.0.1 | Host builder abstractions |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | Service container integration |

> [!NOTE]
> v9 configures Serilog **directly** — the `Albatross.Logging` dependency used in v8 has been removed.

## When to Use (and When Not To)

**Use Albatross.CommandLine.Defaults when:**
- Building a standalone CLI application
- You want sensible, file-based logging defaults without boilerplate
- Serilog is your preferred logging framework
- You are using `Albatross.Config` `IApplicationPath` for path management

**Configure services manually when:**
- You have specific dependency version requirements
- You need a different logging framework (NLog, log4net, etc.)
- You want console logging or a custom sink topology out of the box
- You need custom configuration providers (Azure Key Vault, AWS Secrets Manager, etc.)
- You are integrating into an existing application with established DI configuration
