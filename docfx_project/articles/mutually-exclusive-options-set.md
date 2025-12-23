# Polymorphic Options: One Handler, Multiple Option Sets

Use a single command action to handle multiple option sets by binding a base options type. Derived options represent mutually exclusive commands that share common properties. This pattern keeps handlers simple while letting each verb define its own options.

## Overview

- One handler type parameter: a base options class.
- Multiple verbs: derived option records that inherit the base.
- The generator wires DI to supply the base type as the concrete derived instance based on the invoked command.
- The handler uses pattern matching to branch per derived type.

## Example

Based on the Sample project’s ExampleSharedCommandOptions.cs:

```csharp
using Albatross.CommandLine;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
    [Verb<ExampleProjectCommandAction>("example project echo", UseBaseOptionsClass = typeof(SharedProjectOptions), Description = "This demonstrates the use of mutually exclusive commands using inheritance.")]
    public record class ProjectEchoOptions : SharedProjectOptions {
        [Option]
        public required int Echo { get; init; }
    }

    [Verb<ExampleProjectCommandAction>("example project fubar", UseBaseOptionsClass = typeof(SharedProjectOptions), Description = "This demonstrates the use of mutually exclusive commands using inheritance.")]
    public record class ProjectFubarOptions : SharedProjectOptions {
        [Option]
        public required int Fubar { get; init; }
    }

    public record class SharedProjectOptions {
        [Option]
        public required int Id { get; init; }
    }

    public class ExampleProjectCommandAction : CommandAction<SharedProjectOptions> {
        public ExampleProjectCommandAction(SharedProjectOptions options) : base(options) {
        }

        public override Task<int> Invoke(CancellationToken cancellationToken) {
            if (options is ProjectEchoOptions echoOptions) {
                this.Writer.WriteLine($"Invoked project echo: {echoOptions}");
            } else if (options is ProjectFubarOptions fubarOptions) {
                this.Writer.WriteLine($"Invoked project fubar: {fubarOptions}");
            } else {
                throw new NotSupportedException($"Unsupported options: {options}");
            }
            return Task.FromResult(0);
        }
    }
}
```

## Under the Hood

The code generator registers the base options type and constructs the appropriate derived instance from the `ParseResult` at runtime:

```csharp
// generated code (simplified)
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

## CLI Usage

```bash
# Echo variant
dotnet run -- example project echo --id 123 --echo 3

# Fubar variant
dotnet run -- example project fubar --id 123 --fubar 9
```

## Benefits

- Clear handler: a single `CommandAction<SharedProjectOptions>` handles all variants.
- Strong typing: each verb has its own derived record with specific options.
- Zero boilerplate wiring: generator binds the correct derived instance via DI.

## Best Practices

- Keep the base type limited to truly shared members only.
- Always set `UseBaseOptionsClass` on verbs that should map to the base.
- In the handler, pattern-match on the derived types and handle each explicitly.

## Troubleshooting

Missing `UseBaseOptionsClass` on a verb prevents the generator from mapping it to the base options type.

```csharp
// ❌ Not mapped to base type
[Verb<ExampleProjectCommandAction>("example project echo")]
public record class ProjectEchoOptions : SharedProjectOptions { }

// ✅ Correct: mapped to base type
[Verb<ExampleProjectCommandAction>("example project echo", UseBaseOptionsClass = typeof(SharedProjectOptions))]
public record class ProjectEchoOptions : SharedProjectOptions { }
```

If your handler type parameter is a derived options type, you lose the ability to handle multiple variants.

```csharp
// ❌ Too specific
public class ProjectCommandAction : CommandAction<ProjectEchoOptions> { }

// ✅ Generic over base options
public class ProjectCommandAction : CommandAction<SharedProjectOptions> { }
```

This pattern is ideal for mutually exclusive sub-commands that share a common identity or context, while keeping each sub-command’s options cleanly separated.
