---
_layout: landing
---

# Albatross.CommandLine

A powerful .NET library that simplifies the creation of command-line applications using [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/). It provides automatic code generation, dependency injection, configuration, and logging support.

## ✨ Key Features

- **🚀 Quick Setup**: Minimal boilerplate with automatic code generation
- **🔄 Dependency Injection**: Built-in DI container support with service registration
- **📝 Logging Integration**: Seamless Serilog integration via [Albatross.Logging](https://www.nuget.org/packages/Albatross.Logging)
- **⚙️ Configuration**: Easy setup with [Albatross.Config](https://www.nuget.org/packages/Albatross.Config)
- **🔧 Full Flexibility**: Leverage the complete power of System.CommandLine while reducing complexity

## 🎯 Why Choose Albatross.CommandLine?

Traditional command-line applications require extensive boilerplate code for option parsing, validation, and service setup. Albatross.CommandLine eliminates this complexity through intelligent code generation while preserving full access to System.CommandLine's capabilities.

## 🚀 Quick Start

### 1. Install the Package

```xml
<PackageReference Include="Albatross.CommandLine" Version="8.0.1" />
```

### 2. Create Your Setup Class

```csharp
public class MySetup : Setup {
    public MySetup() : base("My awesome command-line app") {
    }

    protected override void RegisterServices(ParseResult result, IConfiguration configuration, 
        EnvironmentSetting envSetting, IServiceCollection services) {
        base.RegisterServices(result, configuration, envSetting, services);
        
        // Auto-generated service registration
        services.RegisterCommands();
        
        // Register your custom services
        services.AddSingleton<IMyService, MyService>();
    }
}
```

### 3. Update Program.cs

```csharp
internal class Program {
    static Task<int> Main(string[] args) {
        return new MySetup()
            .AddCommands()  // Auto-generated method
            .Parse(args)
            .RegisterServices()
            .Build()
            .InvokeAsync();
    }
}

### 4. Create Your First Command

```csharp
[Verb<HelloWorldCommandAction>("hello", Description = "The HelloWorld command")]
public record class HelloWorldOptions {
    [Argument(Description = "The order of declaration determines the position of the argument")]
    public required string Argument1 { get; init; }

    [Argument(Description = "Optional arguments should be put after the required ones")]
    public string? Argument2 { get; init; }

    [Option(Description = "By default, nullability of the property is used to determine if the option is required")]
    public required string Name { get; init; }

    [Option(Description = "Same goes for the value types")]
    public required int Value { get; init; }

    [Option(DefaultToInitializer = true, Description = "Set DefaultToInitializer to true to use the property initializer as the default value")]
    public DateOnly Date { get; init; } = DateOnly.FromDateTime(DateTime.Today);
}

public class HelloWorldCommandAction : CommandAction<HelloWorldOptions> {
    public HelloWorldCommandAction(HelloWorldOptions options) : base(options) {
    }

    public override async Task<int> Invoke(CancellationToken cancellationToken) {
        await this.Writer.WriteLineAsync(options.ToString());
        return 0;
    }
}
```

### 5. Run Your Application

```bash
dotnet run -- hello "first-arg" --name "World" --value 42
```

Output:
```
HelloWorldOptions { Argument1 = first-arg, Argument2 = , Name = World, Value = 42, NumericValue = 0, Date = 2025-12-22 }
```

## 🎉 What Just Happened?

The code generator automatically created:

- ✅ **HelloWorldCommand.g.cs** - A complete System.CommandLine command class
- ✅ **Service Registration** - Dependency injection setup for your command and options  
- ✅ **Command Registration** - Integration with the command builder

All with **zero manual configuration**!

## 📚 Learn More

- **[Code Generator](articles/code-generator.md)** - Deep dive into automatic code generation
- **[Conventions](../docs/command-options.md)** - Working with options and arguments
- **[Sub Commands](../docs/sub-commands.md)** - Building hierarchical commands
- **[Dependency Injection](../docs/dependency-injection.md)** - Service registration patterns

## 🔧 Prerequisites
- **C# Compiler 4.10.0+** (included with .NET 8 SDK or Visual Studio 2022)