# Mutually Exclusive Parameter Set

The Mutually Exclusive Parameter Set pattern enables a single command handler to work with multiple, distinct sets of parameters. This is achieved by creating several parameters classes that inherit from a common base class containing shared properties. The command handler receives the base class via dependency injection and uses pattern matching to determine the specific derived type at runtime, allowing it to execute the appropriate logic.

This pattern is useful when you have several similar commands (e.g., `codegen csharp`, `codegen typescript`) that share common options but also have their own specific parameters.


## Example
```csharp
using Albatross.CommandLine;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
    [Verb<CodeGenHandler>("codegen csharp", BaseParamsClass = typeof(CodeGenParams), Description = "CSharp Code Generator")]
    public record class CSharpCodeGenParams : CodeGenParams {
        // CSharp specific parameters
        ...
    }

    [Verb<CodeGenHandler>("codegen typescript", BaseParamsClass = typeof(CodeGenParams), Description = "TypeScript Code Generator")]
    public record class TypeScritpCodeGenParams : CodeGenParams {
        // TypeScript specific parameters
        ,,,
    }

    public record class CodeGenParams {
        // Shared parameters
        [Option]
        public required FileInfo CsProjectFile { get; init; }
    }

    public class CodeGenHandler : IAsyncCommandHandler<CodeGenParams> {
        CodeGenParams parameters;
        public CodeGenHandler(CodeGenParams parameters) {
            this.parameters = parameters;
        }

        public async Task<int> InvokeAsync(CancellationToken cancellationToken) {
            if (this.parameters is CSharpCodeGenParams csharpParams) {
                // doing C# codegen
                ...
            } else if (this.parameters is TypeScriptCodeGenParams tyepscriptParams) {
                // doing Typescirpt codegen
                ...
            } else {
                throw new NotSupportedException($"Unsupported parameters type: {this.parameters.GetType()}");
            }
            return 0;
        }
    }
}
```

## Under the Hood

The `Albatross.CommandLine` source generator automates the dependency injection wiring for this pattern. It generates code that registers the base parameter type (`CodeGenParams`) with the DI container. This registration includes a factory function that inspects the `ParseResult` to determine which command was invoked. Based on the command, it instantiates and populates the correct derived parameter class (`CSharpCodeGenParams` or `TypeScriptCodeGenParams`) and returns it as the base type.

This allows the `CodeGenHandler` to be constructed with the correct, fully populated parameter object without needing to know the details of command parsing.

```csharp
// generated code (simplified)
services.AddScoped<CodeGenParams>(provider => {
    var result = provider.GetRequiredService<ParseResult>();
    var key = result.CommandResult.Command.GetCommandKey();
    return key switch {
        "codegen csharp" => new CSharpCodeGenParams() {
            // csharp params initialization
            ...
        },
        "codegen typescript" => new TypeScriptCodeGenParams() {
            // typescript params initialization
            ...
        },
        _ => throw new System.InvalidOperationException($"Command {key} is not registered for base Params class \"CodeGenParams\"")
    };
});
```

