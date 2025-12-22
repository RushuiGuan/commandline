# Customizing Commands with Partial Classes

Albatross.CommandLine generates partial command classes that can be extended to add custom behavior, validation, and configuration. This allows you to leverage both code generation benefits and custom logic.

## How Command Customization Works

When the code generator creates a command from your options class, it generates a partial class. You can extend this partial class to add custom functionality by implementing the `Initialize()` partial method or adding custom properties and methods.

## Basic Customization Example

Here's a simple example of customizing a generated command:

```csharp
[Verb<DefaultCommandAction<TestCustomizedCommandOptions>>("test customized", Description = "Commands can be customized by extending its partial class")]
public record class TestCustomizedCommandOptions {
    [Option("d")]
    public required string Description { get; init; }
}

// Extend the generated partial class
public partial class TestCustomizedCommand {
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

## The Initialize() Partial Method

The `Initialize()` partial method is called during command construction and is the primary extension point for customizing generated commands. Within this method, you can:

- Add validators to options and arguments
- Modify option properties
- Set up custom behavior
- Configure advanced System.CommandLine features

## Adding Validation

You can add custom validation logic to any option or argument:

```csharp
public partial class HelloWorldCommand {
    partial void Initialize() {
        // Date validation
        this.Option_Date.Validators.Add(r => {
            var value = r.GetValue(this.Option_Date);
            if(value < DateOnly.FromDateTime(DateTime.Today)) {
                r.AddError($"Invalid value {value:yyyy-MM-dd} for Date. It cannot be in the past");
            }
        });

        // String length validation
        this.Option_Name.Validators.Add(r => {
            var name = r.GetValue(this.Option_Name);
            if (name != null && name.Length > 50) {
                r.AddError("Name cannot exceed 50 characters");
            }
        });

        // Numeric range validation
        this.Option_Value.Validators.Add(r => {
            var value = r.GetValue(this.Option_Value);
            if (value < 1 || value > 100) {
                r.AddError("Value must be between 1 and 100");
            }
        });
    }
}
```

## Complex Validation Scenarios

For more complex validation that involves multiple options or cross-field validation:

```csharp
public partial class ProjectCommand {
    partial void Initialize() {
        // Cross-field validation
        this.Validators.Add(result => {
            var startDate = result.GetValue(this.Option_StartDate);
            var endDate = result.GetValue(this.Option_EndDate);
            
            if (startDate.HasValue && endDate.HasValue && startDate > endDate) {
                result.AddError("Start date cannot be after end date");
            }
        });

        // Conditional requirements
        this.Validators.Add(result => {
            var useDatabase = result.GetValue(this.Option_UseDatabase);
            var connectionString = result.GetValue(this.Option_ConnectionString);
            
            if (useDatabase && string.IsNullOrEmpty(connectionString)) {
                result.AddError("Connection string is required when using database");
            }
        });
    }
}
```

## Accessing Generated Properties

Generated command classes expose options and arguments through properties with specific naming patterns:

- Options: `Option_PropertyName`
- Arguments: `Argument_PropertyName`

```csharp
public partial class BackupCommand {
    partial void Initialize() {
        // Access the generated option property
        this.Option_OutputPath.Validators.Add(r => {
            var path = r.GetValue(this.Option_OutputPath);
            if (path != null && !Directory.Exists(Path.GetDirectoryName(path))) {
                r.AddError($"Directory does not exist: {Path.GetDirectoryName(path)}");
            }
        });

        // Access the generated argument property
        this.Argument_SourceFile.Validators.Add(r => {
            var file = r.GetValue(this.Argument_SourceFile);
            if (!File.Exists(file)) {
                r.AddError($"Source file does not exist: {file}");
            }
        });
    }
}
```

## Adding Custom Methods and Properties

You can extend the partial class with additional methods and properties:

```csharp
public partial class DeployCommand {
    // Custom property
    public string DeploymentId { get; private set; } = Guid.NewGuid().ToString();

    partial void Initialize() {
        // Custom initialization logic
        this.Option_Environment.Validators.Add(r => {
            var env = r.GetValue(this.Option_Environment);
            if (!IsValidEnvironment(env)) {
                r.AddError($"Invalid environment: {env}. Valid environments are: dev, staging, prod");
            }
        });
    }

    // Custom validation method
    private bool IsValidEnvironment(string? environment) {
        var validEnvironments = new[] { "dev", "staging", "prod" };
        return environment != null && validEnvironments.Contains(environment.ToLower());
    }

    // Custom helper method
    public void LogDeploymentStart() {
        Console.WriteLine($"Starting deployment {DeploymentId}");
    }
}
```

## Advanced System.CommandLine Integration

You can leverage advanced System.CommandLine features through customization:

```csharp
public partial class AdvancedCommand {
    partial void Initialize() {
        // Add custom completions
        this.Option_Environment.Completions.Add(ctx => {
            return new[] { "development", "staging", "production" }
                .Where(env => env.StartsWith(ctx.WordToComplete, StringComparison.OrdinalIgnoreCase))
                .Select(env => new CompletionItem(env));
        });

        // Set custom argument parsers
        this.Option_ConfigFile.ArgumentParser = new FileInfoArgumentParser();

        // Add global validators
        this.Validators.Add(result => {
            // Global command validation logic
            ValidateGlobalConstraints(result);
        });
    }

    private void ValidateGlobalConstraints(CommandResult result) {
        // Implementation of global validation
    }
}
```

## Best Practices

1. **Keep customization focused**: Only add customization where it's truly needed.

2. **Use meaningful validation messages**: Provide clear, actionable error messages.

3. **Organize complex validation**: Break complex validation logic into separate methods.

4. **Test customized commands**: Ensure your customizations work as expected with various inputs.

5. **Document custom behavior**: Add comments explaining non-obvious customizations.

## Example: File Processing Command

Here's a complete example showing various customization techniques:

```csharp
[Verb<ProcessFileCommandAction>("process-file", Description = "Process files with validation and custom behavior")]
public record class ProcessFileOptions {
    [Argument(Description = "Input file to process")]
    public required FileInfo InputFile { get; init; }

    [Option(Description = "Output directory")]
    public DirectoryInfo? OutputDirectory { get; init; }

    [Option(Description = "Processing mode")]
    public ProcessingMode Mode { get; init; } = ProcessingMode.Normal;

    [Option(Description = "Maximum file size in MB")]
    public int MaxSizeMB { get; init; } = 100;
}

public partial class ProcessFileCommand {
    partial void Initialize() {
        // File existence validation
        this.Argument_InputFile.Validators.Add(r => {
            var file = r.GetValue(this.Argument_InputFile);
            if (file != null && !file.Exists) {
                r.AddError($"Input file does not exist: {file.FullName}");
            }
        });

        // File size validation
        this.Argument_InputFile.Validators.Add(r => {
            var file = r.GetValue(this.Argument_InputFile);
            var maxSize = r.GetValue(this.Option_MaxSizeMB);
            
            if (file?.Exists == true) {
                var sizeInMB = file.Length / (1024 * 1024);
                if (sizeInMB > maxSize) {
                    r.AddError($"File size ({sizeInMB}MB) exceeds maximum ({maxSize}MB)");
                }
            }
        });

        // Output directory validation
        this.Option_OutputDirectory.Validators.Add(r => {
            var dir = r.GetValue(this.Option_OutputDirectory);
            if (dir != null && !dir.Exists) {
                try {
                    dir.Create();
                } catch (Exception ex) {
                    r.AddError($"Cannot create output directory: {ex.Message}");
                }
            }
        });

        // Cross-option validation
        this.Validators.Add(result => {
            var inputFile = result.GetValue(this.Argument_InputFile);
            var outputDir = result.GetValue(this.Option_OutputDirectory);
            
            if (inputFile?.Directory?.FullName == outputDir?.FullName) {
                result.AddError("Output directory cannot be the same as input file directory");
            }
        });
    }
}

public enum ProcessingMode {
    Normal,
    Fast,
    Thorough
}
```

This comprehensive example demonstrates input validation, cross-field validation, directory creation, and various error scenarios that can be handled through command customization.
