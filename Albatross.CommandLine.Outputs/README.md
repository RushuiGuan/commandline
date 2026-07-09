# Albatross.CommandLine.Outputs

Renders CLI command results as stable, queryable JSON so a command's output is a reliable contract for humans, shell scripts, and AI/automation alike. Built for `Albatross.CommandLine`, it keeps standard output clean: a single predictable result shape, JmesPath querying at the call site, and terminal color that is stripped automatically when the output is piped.

## Key Features
- **Standard result envelope** - `CommandOutput` and `CommandOutput<T>` give every command one predictable shape for both success and failure: command key, message, error and detail, exit code, optional log folder, and typed `Data`.
- **Stable schema over compactness** - The shared serializer drops nulls but keeps default values and empty collections, so a consumer never has to disambiguate "empty" from "absent" — collections are always arrays, and no field has to be inferred from a missing key.
- **Built-in JmesPath querying** - The `--query`/`-q` option shapes output per call. The expression is parsed and validated up front through an injectable option handler, so invalid syntax fails fast instead of at render time.
- **Pipe- and AI-safe rendering** - Objects and arrays print indented and syntax-highlighted on a terminal; ANSI color is stripped automatically when redirected or piped, and scalar results print raw so scripts can capture them directly.
- **Compact mode** - The `--compact` option emits minified single-line JSON with no indentation or color.

## Quick Start
Add the query and compact options to a command's parameters, then write the result through the `Print` extension:

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Outputs;
using DevLab.JmesPath.Expressions;

[Verb<GetWidgetsHandler>("get widgets")]
public record class GetWidgetsParams {
    [UseOption<QueryOption>]
    public JmesPathExpression? Query { get; init; }

    [UseOption<CompactOption>]
    public bool Compact { get; init; }
}

public class GetWidgetsHandler : IAsyncCommandHandler {
    readonly GetWidgetsParams parameters;
    readonly IWidgetService service;

    public GetWidgetsHandler(GetWidgetsParams parameters, IWidgetService service) {
        this.parameters = parameters;
        this.service = service;
    }

    public async Task<int> InvokeAsync(CancellationToken cancellationToken) {
        var widgets = await service.GetAll(cancellationToken);
        widgets.Print(parameters.Query, parameters.Compact);
        return 0;
    }
}
```

The options then shape output at the call site:

```bash
myapp get widgets                     # indented, colored JSON array
myapp get widgets --query "[].name"   # just the names, shaped by JmesPath
myapp get widgets --compact           # single-line JSON
```

For commands that report their own outcome instead of returning data, use `ParseResult.PrintSuccess(...)` and `ParseResult.PrintError(...)`, which emit a `CommandOutput` envelope to stdout and stderr respectively.

## Dependencies
- Albatross.CommandLine 9.0.0
- System.CommandLine 3.0.0-preview.5.26302.115 (prerelease)
- Newtonsoft.Json 13.0.4
- Spectre.Console 0.57.2
- Spectre.Console.Json 0.57.2
- JmesPath.Net 1.1.0

## Prerequisites
- .NET 8.0 or later.

## Documentation

**[Complete Documentation](https://rushuiguan.github.io/commandline/)**

### Links
- **[Option Pre-processing and Transformation](https://rushuiguan.github.io/commandline/articles/option-preprocessing-transformation.html)** — how `--query` parses its input and injects a compiled JmesPath expression into the handler.
- **[Reusable Parameters](https://rushuiguan.github.io/commandline/articles/reusable-parameter.html)** — applying `QueryOption` and `CompactOption` to a command with `[UseOption<T>]`.
