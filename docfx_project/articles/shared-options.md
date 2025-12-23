# Shared Options

Albatross.CommandLine supports sharing common options across multiple related commands through base options classes. This feature allows you to define common properties once and reuse them across different commands while maintaining type safety and code organization.

## Overview

Shared options are useful when you have a family of related commands that need common configuration properties. Instead of duplicating the same options across multiple command classes, you can define a base options class and inherit from it.

## Key Concepts

- **Base Options Class**: A class containing common options shared across commands
- **UseBaseOptionsClass**: Attribute property that specifies which base class to use for shared options
- **Inheritance**: Derived options classes inherit from the base options class
- **Polymorphic Handling**: Command actions can handle different derived option types

## Basic Usage

### 1. Define a Shared Base Options Class

```csharp
public record class SharedProjectOptions {
    [Option(Description = "Project identifier")]
    public required int Id { get; init; }
    
    [Option(Description = "Project directory path")]
    public string? ProjectPath { get; init; }
    
    [Option(Description = "Enable verbose output")]
    public bool Verbose { get; init; }
}
```

### 2. Create Derived Options Classes

Each command that needs the shared options inherits from the base class and adds its own specific options:

```csharp
[Verb<ExampleProjectCommandAction>("example project echo", 
    UseBaseOptionsClass = typeof(SharedProjectOptions), 
    Description = "Echo command with shared project options")]
public record class ProjectEchoOptions : SharedProjectOptions {
    [Option(Description = "Number of times to echo")]
    public required int Echo { get; init; }
    
    [Option(Description = "Message to echo")]
    public string Message { get; init; } = "Hello";
}

[Verb<ExampleProjectCommandAction>("example project build", 
    UseBaseOptionsClass = typeof(SharedProjectOptions), 
    Description = "Build command with shared project options")]
public record class ProjectBuildOptions : SharedProjectOptions {
    [Option(Description = "Build configuration")]
    public string Configuration { get; init; } = "Release";
    
    [Option(Description = "Skip tests during build")]
    public bool SkipTests { get; init; }
}
```

### 3. Create a Polymorphic Command Action

The command action accepts the base options type and uses pattern matching to handle specific derived types:

```csharp
public class ExampleProjectCommandAction : CommandAction<SharedProjectOptions> {
    public ExampleProjectCommandAction(SharedProjectOptions options) : base(options) {
    }

    public override Task<int> Invoke(CancellationToken cancellationToken) {
        // Access shared options
        await this.Writer.WriteLineAsync($"Project ID: {options.Id}");
        if (options.ProjectPath != null) {
            await this.Writer.WriteLineAsync($"Project Path: {options.ProjectPath}");
        }
        
        // Handle specific command types
        if (options is ProjectEchoOptions echoOptions) {
            for (int i = 0; i < echoOptions.Echo; i++) {
                await this.Writer.WriteLineAsync(echoOptions.Message);
            }
        } else if (options is ProjectBuildOptions buildOptions) {
            await this.Writer.WriteLineAsync($"Building in {buildOptions.Configuration} mode");
            if (buildOptions.SkipTests) {
                await this.Writer.WriteLineAsync("Skipping tests");
            }
        } else {
            throw new NotSupportedException($"Unsupported options: {options}");
        }
        
        return 0;
    }
}
```

## Under the Hood
Code generator automatically registers the Shared Options class based on the parse result:
```csharp
// generated code
services.AddScoped<Sample.CommandLine.SharedProjectOptions>(provider => {
	var result = provider.GetRequiredService<ParseResult>();
	var key = result.CommandResult.Command.GetCommandKey();
	return key switch {
		"example project echo" => new Sample.CommandLine.ProjectEchoOptions() {
			Echo = result.GetRequiredValue<int>("--echo"),
			Id = result.GetRequiredValue<int>("--id"),
		},
		"example project fubar" => new Sample.CommandLine.ProjectFubarOptions() {
			Fubar = result.GetRequiredValue<int>("--fubar"),
			Id = result.GetRequiredValue<int>("--id"),
		},
		_ => throw new System.InvalidOperationException($"Command {key} is not registered for base Options class \"SharedProjectOptions\"")
	};
});
```

## Command Line Usage

When you use shared options, all the shared properties are available as command-line options:

```bash
# Using the project echo command
dotnet run -- example project echo --id 123 --project-path "./myproject" --verbose --echo 3 --message "Hello World"

# Using the project build command  
dotnet run -- example project build --id 123 --project-path "./myproject" --configuration Debug --skip-tests
```
## Benefits

### Code Reuse
- Define common options once
- Maintain consistency across related commands
- Reduce duplication and maintenance overhead

### Type Safety
- Full compile-time type checking
- IntelliSense support for all properties
- Pattern matching for specific command types

### Flexibility
- Each command can add its own specific options
- Polymorphic command actions handle different types
- Support for complex inheritance hierarchies

### Maintainability
- Changes to shared options propagate automatically
- Easy to add new commands with shared behavior
- Clear separation of concerns

## Best Practices

1. **Group Related Commands**: Use shared options for commands that logically belong together
2. **Keep Base Classes Focused**: Include only truly shared options in base classes
3. **Use Descriptive Names**: Make base class names clearly indicate their purpose
4. **Document Inheritance**: Use clear descriptions to explain the relationship
5. **Handle All Cases**: Always include pattern matching for all derived types in command actions

## Troubleshooting

### Common Issues

**Missing UseBaseOptionsClass Attribute**
```csharp
// ❌ Wrong - will not include shared options
[Verb<ProjectCommandAction>("project echo")]
public record class ProjectEchoOptions : SharedProjectOptions { }

// ✅ Correct - includes shared options
[Verb<ProjectCommandAction>("project echo", UseBaseOptionsClass = typeof(SharedProjectOptions))]
public record class ProjectEchoOptions : SharedProjectOptions { }
```

**Mismatched Command Action Type**
```csharp
// ❌ Wrong - action expects base type but gets derived type
public class ProjectCommandAction : CommandAction<ProjectEchoOptions> { }

// ✅ Correct - action expects base type, can handle any derived type  
public class ProjectCommandAction : CommandAction<SharedProjectOptions> { }
```

**Missing Pattern Matching**
```csharp
// ❌ Wrong - doesn't handle specific derived types
public override Task<int> Invoke(CancellationToken cancellationToken) {
    // Only accesses shared properties, ignores command-specific ones
    await this.Writer.WriteLineAsync($"ID: {options.Id}");
    return 0;
}

// ✅ Correct - handles all derived types appropriately
public override Task<int> Invoke(CancellationToken cancellationToken) {
    if (options is ProjectEchoOptions echo) {
        // Handle echo-specific logic
    } else if (options is ProjectBuildOptions build) {
        // Handle build-specific logic  
    }
    return 0;
}
```

Shared options provide a powerful way to create consistent, maintainable command-line interfaces while preserving the flexibility to customize individual commands as needed.
