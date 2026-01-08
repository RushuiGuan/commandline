# Albatross.CommandLine.Inputs

This project provides a collection of reusable `System.CommandLine` arguments and options.

## Arguments

-   `InputDirectoryArgument`: An argument that requires a path to an existing directory.
-   `InputFileArgument`: An argument that requires a path to an existing file.
-   `OutputDirectoryArgument`: An argument for specifying an output directory.
-   `OutputFileArgument`: An argument for specifying an output file.

## Options

-   `InputDirectoryOption`: An option that accepts a path to an existing directory.
-   `InputFileOption`: An option that accepts a path to an existing file.
-   `OutputDirectoryOption`: An option for specifying an output directory.
-   `OutputDirectoryWithAutoCreateOption`: An option for specifying an output directory, with the added feature of automatically creating the directory if it does not exist.
-   `OutputFileOption`: An option for specifying an output file.
-   `FormatExpressionOption`: An option that contains a formatting expression that can shape the output of values.  The syntax for the expression can be found [here](https://github.com/RushuiGuan/text/blob/main/Albatross.Text.CliFormat/cheat-sheet.md).

## Usage

```csharp
[Verb<HelloWorldParams, HelloWorldHandler>("hello world")]
public record class HelloWorldParams {
    [UseOption<InputFileOption>]
    public required FileInfo Input { get; init; }
}

public class HelloWorldHandler : IAsyncCommandHandler {
    HelloWorldParams parameters;
    public HelloWorldHandler(HelloWorldParams parameters) {
        this.parameters = parameters;
    }
    public async Task<int> InvokeAsync(CancellationToken cancellationToken) {
        using(var stream = this.parameters.Input.OpenRead()) {
            // do work
        }
        return 0;
    }
}
```
