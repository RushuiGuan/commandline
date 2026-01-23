# Albatross.CommandLine.Defaults

The `Albatross.CommandLine.Defaults` package provides pre-configured integrations for configuration management and Serilog logging. It eliminates boilerplate setup by providing sensible defaults for most command-line scenarios.

## Design Philosophy

### The Core Library: Maximum Compatibility

The main `Albatross.CommandLine` library is designed with **broad compatibility** as its primary goal. It targets .NET Standard 2.1 and carefully selects dependency versions that work across the widest range of .NET applications:

```xml
<!-- Albatross.CommandLine dependencies -->
<PackageReference Include="System.CommandLine" Version="2.0.2" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
```

This conservative approach ensures that:
- Applications targeting .NET 6, .NET 7, .NET 8, or newer can all consume the library
- Version conflicts with other dependencies in your project are minimized
- The library integrates smoothly into existing projects with established dependency trees

### The Defaults Library: Latest and Greatest

In contrast, `Albatross.CommandLine.Defaults` follows a **forward-looking philosophy**. It intentionally uses the latest stable versions of its dependencies:

```xml
<!-- Albatross.CommandLine.Defaults dependencies -->
<PackageReference Include="Albatross.Config" Version="7.5.11" />
<PackageReference Include="Albatross.Logging" Version="10.0.2" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.1" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.1" />
```

This approach reflects the reality that:
- CLI applications are typically standalone executables with full control over their dependency graph
- New projects benefit from the latest features, performance improvements, and security patches
- The Defaults package is optional - users who need older versions can configure logging and configuration manually

**The trade-off is intentional**: if you need to maintain compatibility with older .NET runtimes or have strict dependency version requirements, use the core `Albatross.CommandLine` library directly and configure services yourself. If you're building a new CLI application and want sensible defaults with modern dependencies, use `Albatross.CommandLine.Defaults`.

## What the Library Provides

The Defaults library provides two main integrations that can be used together or separately:

1. **Configuration Management** via `WithConfig()`
2. **Serilog Logging** via `WithSerilog()`
3. **Combined Setup** via `WithDefaults()` (calls both)

## Quick Start

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Defaults;

static async Task<int> Main(string[] args) {
    await using var host = new CommandHost("My CLI App")
        .RegisterServices(RegisterServices)
        .AddCommands()
        .Parse(args)
        .WithDefaults()  // Adds config + logging
        .Build();
    return await host.InvokeAsync();
}
```

## Extension Methods

### WithDefaults()

The `WithDefaults()` extension method is a convenience method that applies both configuration and Serilog logging:

```csharp
public static CommandHost WithDefaults(this CommandHost commandHost)
    => commandHost.WithConfig().WithSerilog();
```

This is the recommended approach for most applications.

### WithConfig()

The `WithConfig()` method sets up JSON-based configuration with environment support:

```csharp
host.Parse(args)
    .WithConfig()
    .Build();
```

**What it configures:**

1. **JSON Configuration Files**
   - Loads `appsettings.json` from the application's base directory
   - Loads environment-specific files like `appsettings.Development.json` or `appsettings.Production.json`
   - Configuration files are optional (won't fail if missing)
   - Files are monitored for changes with `reloadOnChange: true`

2. **Environment Variables**
   - Adds the environment variable configuration provider
   - Environment variables can override JSON settings

3. **Environment Detection**
   - Uses the `DOTNET_ENVIRONMENT` environment variable to determine which environment-specific config file to load
   - Common values: `Development`, `Staging`, `Production`

4. **Registered Services**
   - `EnvironmentSetting` - Provides access to the current environment name
   - `ProgramSetting` - Wraps the `IConfiguration` instance
   - `IHostEnvironment` - Standard host environment abstraction

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

The `WithSerilog()` method configures Serilog as the logging provider with console output:

```csharp
host.Parse(args)
    .WithSerilog()
    .Build();
```

**What it configures:**

1. **Serilog Integration**
   - Registers Serilog as the logging provider via `UseSerilog()`
   - Uses the [Albatross.Logging](https://github.com/RushuiGuan/config/tree/main/Albatross.Logging) library for setup

2. **File-Based Configuration**
   - Loads Serilog settings from `serilog.json` if present
   - Supports environment-specific files like `serilog.Development.json`

3. **Console Output**
   - Configures console logging with the appropriate minimum level
   - Log level is determined by the `--verbosity` command-line option

4. **Verbosity Integration**
   - Reads the `--verbosity` option value from the parse result
   - Maps `LogLevel` to Serilog's `LogEventLevel`:

| LogLevel | Serilog Level |
|----------|---------------|
| Trace | Verbose |
| Debug | Debug |
| Information | Information |
| Warning | Warning |
| Error | Error |
| Critical | Fatal |
| None | (logging disabled) |

**Important**: `WithSerilog()` must be called **after** `Parse()` because it reads the verbosity option from the parse result.

## Order of Operations

The extension methods must be called in the correct order:

```csharp
await using var host = new CommandHost("My App")
    .RegisterServices(RegisterServices)  // 1. Register your services
    .AddCommands()                        // 2. Add generated commands
    .Parse(args)                          // 3. Parse command line
    .WithDefaults()                       // 4. Configure defaults (after parse!)
    .Build();                             // 5. Build the host
```

`WithDefaults()`, `WithConfig()`, and `WithSerilog()` must be called after `Parse()` because:
- `WithSerilog()` reads the `--verbosity` option from the parse result
- Configuration should be available before the host is built

## Using Components Separately

If you only need one of the integrations:

```csharp
// Configuration only (no logging)
host.Parse(args)
    .WithConfig()
    .Build();

// Logging only (no configuration)
host.Parse(args)
    .WithSerilog()
    .Build();
```

## Dependencies

The Defaults package brings in these libraries:

| Package | Purpose |
|---------|---------|
| [Albatross.Config](https://github.com/RushuiGuan/config/tree/main/Albatross.Config) | Configuration management, settings binding, environment handling |
| [Albatross.Logging](https://github.com/RushuiGuan/config/tree/main/Albatross.Logging) | Serilog setup utilities, console and file sink configuration |
| Microsoft.Extensions.Hosting | Host builder abstractions |
| Microsoft.Extensions.DependencyInjection | Service container integration |

## When to Use (and When Not To)

**Use Albatross.CommandLine.Defaults when:**
- Building a new standalone CLI application
- You want sensible defaults without boilerplate
- Using the latest .NET runtime (e.g., .NET 8 or newer)
- Serilog is your preferred logging framework

**Configure services manually when:**
- You have specific dependency version requirements
- You need a different logging framework (NLog, log4net, etc.)
- You need custom configuration providers (Azure Key Vault, AWS Secrets Manager, etc.)
- You're integrating into an existing application with established DI configuration
