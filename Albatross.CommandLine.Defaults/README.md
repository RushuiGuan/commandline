# Albatross.CommandLine.Defaults

**Optional package providing default integrations for common command-line application patterns**

This package adds pre-configured integrations for configuration management and logging to Albatross.CommandLine applications. It eliminates boilerplate setup by providing sensible defaults for most command-line scenarios.

## Quick Start

Install the package alongside Albatross.CommandLine:

```xml
<PackageReference Include="Albatross.CommandLine.Defaults" Version="..." />
```

Add `.WithDefaults()` to your command host setup:

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Defaults;

static async Task<int> Main(string[] args) {
    await using var host = new CommandHost("My CLI App")
        .RegisterServices(RegisterServices)
        .AddCommands()
        .Parse(args)
        .WithDefaults()  // <-- Adds config + logging defaults
        .Build();
    return await host.InvokeAsync();
}
```

## What's Included

### Configuration Support
- **JSON Configuration**: Automatically loads `appsettings.json` and environment-specific variants
- **Environment Variables**: Includes environment variable configuration provider  
- **Environment Detection**: Uses `DOTNET_ENVIRONMENT` for configuration file selection
- **DI Integration**: Registers configuration services for dependency injection
- **Additional Documentation**: [Albatross.Config](https://github.com/RushuiGuan/config/tree/main/Albatross.Config)

### Serilog Integration
- **Console Logging**: Pre-configured console output with appropriate formatting
- **Verbosity Control**: Respects the built-in `--verbosity` option from CommandBuilder
- **Level Mapping**: Maps Microsoft.Extensions.Logging levels to Serilog levels
- **Zero Configuration**: Works out-of-the-box with sensible defaults
- **Additional Documentation**: [Albatross.Logging](https://github.com/RushuiGuan/config/tree/main/Albatross.Logging)


### Verbosity-Aware Logging

The built-in `--verbosity` option automatically configures Serilog output levels:

```bash
# No logging output
dotnet run -- my-command --verbosity None

# Only errors and warnings  
dotnet run -- my-command --verbosity Error

# Full debug output
dotnet run -- my-command --verbosity Debug
```

Level mapping:
- `None` → No logging output
- `Critical` → Serilog Fatal
- `Error` → Serilog Error  
- `Warning` → Serilog Warning
- `Information` → Serilog Information
- `Debug` → Serilog Debug
- `Trace` → Serilog Verbose

## Individual Extensions

You can use components separately instead of `.WithDefaults()`:

```csharp
await using var host = new CommandHost("My CLI App")
    .Parse(args)
    .WithConfig()    // Configuration only
    .WithSerilog()   // Logging only
    .Build();
```

### WithConfig()
- Loads appsettings.json files based on environment
- Adds environment variable support
- Registers `IConfiguration`, `ProgramSetting`, and `IHostEnvironment` services

### WithSerilog()  
- Configures Serilog with console output
- Maps verbosity levels appropriately
- Uses the `--verbosity` option value

## Dependencies

This package includes these integrations:

- **Albatross.Config** (7.5.11): Configuration management and settings binding
- **Albatross.Logging** (10.0.1): Serilog setup and configuration helpers
- **Microsoft.Extensions.Hosting** (10.0.1): Host builder and environment abstractions
- **Microsoft.Extensions.DependencyInjection** (10.0.1): Service container integration

## When to Use

**Use Albatross.CommandLine.Defaults when:**
- You want JSON configuration file support
- You need structured logging with Serilog  
- You want verbosity control to work automatically
- You prefer convention over configuration

**Skip this package when:**
- You need custom logging providers (not Serilog)
- You have complex configuration requirements
- You want full control over service registration
- Your CLI doesn't need configuration files

## Target Framework

- **.NET Standard 2.1**: Compatible with .NET 5+ and .NET Framework 4.8+