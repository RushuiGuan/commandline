# Albatross.CommandLine.Outputs

The `Albatross.CommandLine.Outputs` package gives a CLI one predictable way to emit its results: a `CommandOutput` envelope, a `Print` extension that renders JSON, a reusable `--compact` option, and an opt-in `GlobalErrorHandler` that reports failures in the same shape.

## Design Philosophy

### Output Is a Contract

In v9 the core library writes nothing to `stdout`/`stderr` on its own, and the [Defaults library](defaults-library.md) logs to a file. What remains on the console is the command's result — and that result is piped to `jq`, captured by scripts, and fed to AI/agent tooling as well as read by people. The package treats it as a contract:

- **Compaction must be lossless.** Null properties are dropped (present-and-null means the same as absent), but **default values and empty collections are kept**: `false`, `0` and `[]` are printed, so a consumer never has to infer anything from a missing key, and collections are always arrays.
- **Shaping belongs to the consumer.** The command emits its full, canonical result. A caller who wants less pipes it through `jq` or a similar tool; there is no built-in query option.
- **Color is free and non-destructive.** JSON is syntax-highlighted on a terminal; ANSI codes are stripped when the output is redirected.
- **Results and errors share one shape.** Success goes to `stdout` and failure to `stderr`, both as a `CommandOutput` envelope.

### A Separate, Opt-in Package

Output strategy is an opinion, so it lives outside the dependency-light core. It is also independent of the Defaults package: an application can take structured output with its own logging, or file logging with its own output. The two are a recommended pairing, not a requirement.

## Quick Start

```bash
dotnet add package Albatross.CommandLine.Outputs --prerelease
```

Add `--compact` to a command's parameters and print the result:

```csharp
using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Outputs;
using System.CommandLine;

[Verb<GetWidgetsHandler>("get widgets", Description = "List widgets")]
public record class GetWidgetsParams {
    [UseOption<CompactOption>]
    public bool Compact { get; init; }
}

public class GetWidgetsHandler : BaseHandler<GetWidgetsParams> {
    readonly IWidgetService service;

    public GetWidgetsHandler(ParseResult result, GetWidgetsParams parameters, IWidgetService service)
        : base(result, parameters) {
        this.service = service;
    }

    public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
        var widgets = await service.GetAll(cancellationToken);
        widgets.Print(parameters.Compact);
        return 0;
    }
}
```

For a large result, or one a program will parse, use `await widgets.PrintAsync(parameters.Compact, cancellationToken)` instead — see [Which one to use](#which-one-to-use).

Register the error handler so failures are reported in the same shape:

```csharp
static void RegisterServices(ParseResult result, IServiceCollection services) {
    services.AddSingleton<ICommandErrorHandler, GlobalErrorHandler>();
    services.RegisterCommands();
}
```

## Printing Data: `Print` and `PrintAsync`

```csharp
public static void Print<T>(this T value, bool compact, bool stderr = false)
public static Task PrintAsync<T>(this T value, bool compact, CancellationToken cancellationToken, bool stderr = false)
```

Both serialize `value` by its runtime type with the shared [serializer options](#serialization) and write it to `stdout` (or `stderr` when `stderr` is `true`). A null value prints nothing. Both decide the same way what to print as JSON:

| Value | Default | `compact: true` |
|---|---|---|
| Object or array | Indented JSON | Minified single-line JSON |
| Scalar (string, number, boolean, date, enum) | Written raw — a string prints without quotes | Same |

Raw scalars let a script capture a single value directly: `id=$(myapp create widget --name foo)`. They are formatted exactly as they would be inside an object — `true`, `2026-11-16`, `1.5` — never by `ToString()`, so the output does not change with the machine's culture.

`Extensions.ShouldPrintJson(value)` exposes the decision: it returns true when the value serializes to a JSON object or array. It is answered from the type alone, without serializing, except for `JsonNode`, `JsonElement` and `JsonDocument`, which are checked by content. A type with a custom converter that writes an object counts as a scalar and prints as single-line JSON.

### Which one to use

| | `Print` | `PrintAsync` |
|---|---|---|
| Color | Syntax-highlighted on a terminal | None |
| Ctrl+C while printing | Not interruptible: the process waits for the render to finish | Stops within one 16K-character slice |
| Indented output when redirected | Can be **invalid JSON** — see below | Always valid JSON |
| Typical use | Small results read by a person | Large or unbounded results, and output read by programs |

```csharp
public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
    var widgets = await service.GetAll(cancellationToken);
    await widgets.PrintAsync(parameters.Compact, cancellationToken);
    return 0;
}
```

When Ctrl+C cancels the token, `PrintAsync` throws `OperationCanceledException`. Let it propagate: the pipeline reports it as a cancellation through the [error handler](custom-error-handler.md) (`GlobalErrorHandler` exits with 130). Output stops mid-document, so `stdout` holds truncated JSON — a consumer of a cancelled command must check the exit code. Serialization happens before the first slice is written and is not interruptible.

> [!IMPORTANT]
> **`Print`'s indented output is not safe to parse.** It is rendered by Spectre.Console, which wraps long lines at the console width even when the output is redirected; a wrap inside a JSON string breaks the JSON. When a program reads the output, use `--compact` or `PrintAsync`, both of which write directly to the stream.

## The `CommandOutput` Envelope

Commands that report an outcome — rather than return a data set — print a `CommandOutput`:

| Property | Type | Description |
|---|---|---|
| `command` | `string` | The command key, e.g. `"widget delete"` |
| `message` | `string?` | Human-readable outcome |
| `errors` | `ErrorOutput[]?` | Present only on failure |
| `exitCode` | `int` | The process exit code |
| `logFolder` | `string?` | Where to find the log files, when known |
| `data` | `T?` | Typed payload (`CommandOutput<T>` only); always the last property |

Four `ParseResult` extensions build and print it. They take the command key from the parse result and honor `--compact` when the command declares it — and are safe when it does not:

```csharp
// success → stdout, exit code 0
result.PrintSuccess($"deleted widget {parameters.Id}");
result.PrintSuccessWithData($"downloaded {files.Count} files", files);

// failure → stderr; returns the exit code (default 1) so the handler can return it
return result.PrintError("widget not found", errorMessage: $"no widget with id {parameters.Id}");
return result.PrintErrorWithData("partial failure", failedItems, exitCode: 2);
```

```json
{
  "command": "widget delete",
  "message": "deleted widget 42",
  "exitCode": 0
}
```

`PrintError` is for failures a handler detects itself. For unexpected failures, throw and let the error handler report them.

## Reporting Errors: `GlobalErrorHandler`

`GlobalErrorHandler` implements `ICommandErrorHandler` (see [Custom Global Error Handler](custom-error-handler.md)). Every failure the pipeline detects — a command handler exception, a failed option handler, a handler that cannot be resolved, a cancellation — is written to `stderr` as a `CommandOutput` with an `errors` array:

```json
{
  "command": "test option-error",
  "errors": [
    {
      "source": "OptionHandler",
      "symbol": "--alpha",
      "message": "Error occurred while processing option [test option-error [--alpha]]",
      "detail": "alpha option handler failed"
    },
    {
      "source": "OptionHandler",
      "symbol": "--charlie",
      "message": "Error occurred while processing option [test option-error [--charlie]]",
      "detail": "charlie option handler failed"
    }
  ],
  "exitCode": 1
}
```

Each `ErrorOutput` carries:

- **`source`** — the pipeline stage: `CommandHandler`, `OptionHandler`, `ServiceRegistration`, `CommandTaskCancellation` or `OptionTaskCancellation`
- **`symbol`** — the option name for option-level errors (omitted for command handler errors, where it would repeat `command`)
- **`message`** — what failed
- **`detail`** — the exception message. When the message is itself a JSON object or array (an HTTP problem-details body, for example), it is embedded as JSON rather than as an escaped string, so a consumer can read into it.

The exit code is `1`, or `130` when every error is a cancellation (the conventional Ctrl+C code). A real error always outranks a cancellation in the same set.

Logging is not the handler's job: each error is already logged where it was caught.

### Customizing

Subclass `GlobalErrorHandler` and override:

- `Convert(Error)` — to change how an error becomes an `ErrorOutput`, e.g. to unwrap a domain exception
- `GetLogFolderPath()` — to fill in `logFolder`, pointing the user at the log files

```csharp
public class MyErrorHandler : GlobalErrorHandler {
    readonly IApplicationPath appPath;

    public MyErrorHandler(ICommandContext context, IApplicationPath appPath) : base(context) {
        this.appPath = appPath;
    }

    protected override string? GetLogFolderPath() => appPath.LogRoot;
}
```

## Serialization

All output is serialized with `System.Text.Json` through two shared, read-only option sets: `Extensions.CompactSerializerOptions` and `Extensions.FormattedSerializerOptions`, which differ only in indentation:

| Setting | Effect |
|---|---|
| camelCase property names | `ExitCode` → `exitCode` |
| `JsonStringEnumConverter` | Enums print as names, not numbers |
| `WhenWritingNull` | Null properties are omitted; defaults and empty collections are kept |
| Relaxed escaping | `<`, `>`, `'`, `+` and non-ASCII characters print as-is, not as `\uXXXX` |

Customize your own result types with `System.Text.Json.Serialization` attributes — `[JsonPropertyName]`, `[JsonIgnore]`, `[JsonPropertyOrder]`. Newtonsoft.Json attributes are ignored. Reuse these options anywhere you need output that matches the CLI's (in tests, for example). Both instances are read-only: changing a setting on them throws `InvalidOperationException`, so build your own `JsonSerializerOptions` when you need different settings.

## Shaping Output

The package has no query option. Request compact output and pipe it to a JSON tool:

```bash
myapp get widgets --compact | jq -r '.[].name'                  # just the names
myapp get widgets --compact | jq '[.[] | select(.active)]'      # filter
myapp widget delete 42 --compact 2>&1 | jq -r '.errors[]?.detail'  # read an error
```

## Applying `--compact` Across Commands

`--compact` is never added automatically. To give every data command the option once, put it on a shared base record:

```csharp
public abstract record class OutputParams {
    [UseOption<CompactOption>]
    public bool Compact { get; init; }
}

[Verb<GetWidgetsHandler>("get widgets")]
public record class GetWidgetsParams : OutputParams { }
```

A params class can inherit a `record` base only if it is itself a `record class`. Commands that only print a status message through `PrintSuccess`/`PrintError` can skip the base: those methods read `--compact` when present and default to indented output when it is not.

## Dependencies

- `Albatross.CommandLine`
- `Spectre.Console` and `Spectre.Console.Json` — terminal rendering and JSON highlighting

Serialization uses `System.Text.Json` from the framework, so the package adds no JSON library of its own.

## When to Use (and When Not To)

**Use it** when a command returns data or an outcome that anything other than a person might read — which is most CLI commands.

**Skip it** when the output is inherently text (a generated file streamed to `stdout`, a log tail) — write those through `BaseHandler.Writer`. You can still register `GlobalErrorHandler` so that failures are reported as JSON on `stderr`.

## Related Articles

- [What's New in v9](whats-new-v9.md) — why the output strategy exists
- [Custom Global Error Handler](custom-error-handler.md) — the error contract `GlobalErrorHandler` implements
- [Defaults Library](defaults-library.md) — file-based logging, the recommended pairing
- [Reusable Parameters](reusable-parameter.md) — how `[UseOption<CompactOption>]` works
