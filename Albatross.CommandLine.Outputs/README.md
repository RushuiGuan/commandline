# Albatross.CommandLine.Outputs

Renders CLI command results as stable JSON so a command's output is a reliable contract for humans, shell scripts, and AI/automation alike. Built for `Albatross.CommandLine` on `System.Text.Json`, it keeps standard output clean: a single predictable result shape, and terminal color that is stripped automatically when the output is piped.

## Key Features
- **Standard result envelope** - `CommandOutput` and `CommandOutput<T>` give every command one predictable shape for both success and failure: command key, message, errors, exit code, optional log folder, and typed `Data`.
- **Stable schema over compactness** - The shared `System.Text.Json` options drop nulls but keep default values and empty collections, so a consumer never has to disambiguate "empty" from "absent" — collections are always arrays, and no field has to be inferred from a missing key.
- **Pipe- and AI-safe rendering** - Objects and arrays print indented and syntax-highlighted on a terminal; ANSI color is stripped automatically when redirected or piped, and scalar results print raw so scripts can capture them directly.
- **Compact mode** - The `--compact` option emits minified single-line JSON with no indentation or color, ready to pipe into `jq` or any other JSON tool.
- **Interruptible printing** - `PrintAsync` writes in slices and checks the cancellation token between them, so Ctrl+C stops a large print promptly; its output is uncolored and always valid JSON, even when indented and redirected.

## Quick Start
Add the compact option to a command's parameters, then write the result through the `Print` extension:

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Outputs;

[Verb<GetWidgetsHandler>("get widgets")]
public record class GetWidgetsParams {
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
        widgets.Print(parameters.Compact);
        return 0;
    }
}
```

```bash
myapp get widgets                                   # indented, colored JSON array
myapp get widgets --compact                         # single-line JSON
myapp get widgets --compact | jq -r '.[].name'      # shape the output with an external tool
```

For large results, or output a program will parse, use `await widgets.PrintAsync(parameters.Compact, cancellationToken)`: it can be interrupted with Ctrl+C, at the cost of color.

Results are serialized with `System.Text.Json` through the read-only `Extensions.CompactSerializerOptions` and `Extensions.FormattedSerializerOptions`, so customize your own types with `System.Text.Json.Serialization` attributes such as `[JsonPropertyName]` and `[JsonIgnore]`.

For commands that report their own outcome instead of returning data, use `ParseResult.PrintSuccess(...)` and `ParseResult.PrintError(...)`, which emit a `CommandOutput` envelope to stdout and stderr respectively.

## Dependencies
- Albatross.CommandLine 9.0.0
- System.CommandLine 3.0.0-rc.3.26522.148 (prerelease)
- Spectre.Console 0.57.2
- Spectre.Console.Json 0.57.2

## Prerequisites
- .NET 10.0 or later.

## Documentation

**[Complete Documentation](https://rushuiguan.github.io/commandline/)**

### Links
- **[Outputs Library](https://rushuiguan.github.io/commandline/articles/outputs-library.html)** — the envelope, `Print` behavior, error reporting, serialization rules, and shaping output with `jq`.
- **[Reusable Parameters](https://rushuiguan.github.io/commandline/articles/reusable-parameter.html)** — applying `CompactOption` to a command with `[UseOption<T>]`.
