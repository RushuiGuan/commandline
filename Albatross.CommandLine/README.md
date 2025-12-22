# Albatross.CommandLine

A powerful .NET library that simplifies creating command-line applications with [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/). It provides automatic code generation, dependency injection, configuration, and logging support while maintaining full access to System.CommandLine's capabilities.

🎉 **Now using System.CommandLine v2 stable release** for improved reliability and long-term support.

## ✨ Key Features

- **🚀 Minimal Boilerplate** - Attribute-based command definition with automatic code generation
- **🔧 Type Safety** - Leverages C# nullable reference types for automatic requirement detection
- **📦 Dependency Injection** - Built-in DI container integration
- **📝 Logging** - Seamless Serilog integration via [Albatross.Logging](https://www.nuget.org/packages/Albatross.Logging)
- **⚙️ Configuration** - Easy appsettings.json support via [Albatross.Config](https://www.nuget.org/packages/Albatross.Config)
- **🎯 Full Flexibility** - Direct access to System.CommandLine when needed

## 📖 Documentation

**📚 [Complete Documentation](https://rushuiguan.github.io/commandline/)**

### Quick Links
- **[Quick Start Guide](https://rushuiguan.github.io/commandline/articles/quick-start.html)** - Verb, Option, and Argument attributes
- **[Code Generator](https://rushuiguan.github.io/commandline/articles/code-generator.html)** - How automatic code generation works
- **[Shared Options](https://rushuiguan.github.io/commandline/articles/shared-options.html)** - Share common options across commands
- **[Manual Commands](https://rushuiguan.github.io/commandline/articles/manual-command.html)** - Direct System.CommandLine integration

## 🔧 Prerequisites

- **C# Compiler 4.10.0+** (included with .NET 9 SDK)

## 🚀 Quick Start

### 1. Install Package

```xml
<PackageReference Include="Albatross.CommandLine" Version="8.0.1" />
```

### 2. Create Setup Class

```csharp
public class MySetup : Setup {
    public MySetup() : base("My awesome CLI app") {
    }

    protected override void RegisterServices(ParseResult result, IConfiguration configuration, 
        EnvironmentSetting envSetting, IServiceCollection services) {
        base.RegisterServices(result, configuration, envSetting, services);
        
        // Auto-generated service registration
        services.RegisterCommands();
        
        // Your services
        services.AddSingleton<IMyService, MyService>();
    }
}
```

### 3. Update Program.cs

```csharp
internal class Program {
    static Task<int> Main(string[] args) {
        return new MySetup()
            .AddCommands()  // Auto-generated
            .Parse(args)
            .RegisterServices()
            .Build()
            .InvokeAsync();
    }
}
```

### 4. Define Your Command

```csharp
[Verb<HelloCommandAction>("hello", Description = "Say hello to someone")]
public record class HelloOptions {
    // Required argument (position 0)
    [Argument(Description = "Name to greet")]
    public required string Name { get; init; }
    
    // Optional option
    [Option(Description = "Number of times to greet")]
    public int Count { get; init; } = 1;
}

public class HelloCommandAction : CommandAction<HelloOptions> {
    public HelloCommandAction(HelloOptions options) : base(options) {
    }

    public override async Task<int> Invoke(CancellationToken cancellationToken) {
        for (int i = 0; i < options.Count; i++) {
            await this.Writer.WriteLineAsync($"Hello, {options.Name}!");
        }
        return 0;
    }
}
```

### 5. Run It!

```bash
dotnet run -- hello "World" --count 3
```

**That's it!** The code generator automatically creates the command class and service registrations.

## 🌟 Advanced Features

### Sub-Commands
```csharp
[Verb<DatabaseBackupAction>("database backup")]
[Verb<DatabaseRestoreAction>("database restore")]
// Creates: database backup, database restore
```

### Shared Options
```csharp
[Verb<ProjectCommandAction>("project build", UseBaseOptionsClass = typeof(ProjectOptions))]
public record class BuildOptions : ProjectOptions {
    [Option] public string Configuration { get; init; } = "Release";
}
```

### Manual Commands
```csharp
// Full System.CommandLine control when needed
setup.CommandBuilder.AddWithParentKey("tools", new CustomCommand());
```

## 🎯 Global Options

The library creates a single recursive option for logging purposes.
- `-v, --verbosity` - Logging level (Error, Warning, Information, Info, Debug, Trace)

## 🤝 Samples
See the [Sample.CommandLine](../Sample.CommandLine/) project for comprehensive examples of all features.

## 📦 Related Packages

- [Albatross.Logging](https://www.nuget.org/packages/Albatross.Logging) - Serilog integration
- [Albatross.Config](https://www.nuget.org/packages/Albatross.Config) - Configuration support

---

**[📖 Read the Full Documentation →](https://rushuiguan.github.io/commandline/)**
