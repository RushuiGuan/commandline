# Mutually Exclusive Parameter Set

The Mutually Exclusive Parameter Set pattern allows a single command handler to accept multiple, distinct sets of parameters that may share common properties.  This approach involves creating multiple parameter classes that derive from a common base class. The command handler is then injected with an instance of this base type, allowing it to use pattern matching on the runtime type to select the correct code path.



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

The code generator registers the base options type and constructs the appropriate derived instance from the `ParseResult` at runtime:

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

