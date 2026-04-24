# Claude Code Skills for Albatross.CommandLine

This repository includes a [Claude Code](https://docs.anthropic.com/en/docs/claude-code) skill that helps you build CLI applications using the Albatross.CommandLine library.

## What is a Claude Code Skill?

A skill is a set of instructions that teaches Claude Code how to perform specific tasks. The `alba-cli` skill provides templates and guidance for:

- Bootstrapping new CLI projects
- Creating new commands with handlers
- Defining subcommand hierarchies
- Creating reusable option and argument types
- Adding async option pre-processing and validation
- Configuring logging and JSON configuration

## Installation

### Option 1: Download from GitHub

1. Create the skills directory in your project (if it doesn't exist):

```bash
mkdir -p .claude/skills
```

2. Download the skill folder:

```bash
curl -L https://github.com/RushuiGuan/commandline/archive/main.tar.gz | tar -xz --strip-components=2 -C .claude/skills commandline-main/skills/alba-cli
```

### Option 2: Manual Download

1. Navigate to [skills/alba-cli](https://github.com/RushuiGuan/commandline/tree/main/skills/alba-cli) on GitHub
2. Download `SKILL.md`
3. Place it in your project at `.claude/skills/alba-cli/SKILL.md`

## Directory Structure

After installation, your project should have:

```
your-project/
├── .claude/
│   └── skills/
│       └── alba-cli/
│           └── SKILL.md
├── src/
│   └── ...
└── ...
```

## Usage

Once installed, invoke the skill in Claude Code by typing:

```
/alba-cli <what you want to do>
```

The skill understands natural language descriptions of your goals. You don't need to memorize specific sub-commands — just describe what you want and Claude Code will apply the correct patterns.

**Examples:**

```
/alba-cli create a new command called "backup" that takes a source and destination directory
/alba-cli bootstrap a new CLI project named MyTool
/alba-cli add a reusable option for validating an API key asynchronously
/alba-cli add a "config get" and "config set" subcommand pair
```

## What the Skill Knows

### Packages

The skill guides you to the right package for each scenario:

| Package | Purpose |
|---------|---------|
| `Albatross.CommandLine` | Core runtime — always required |
| `Albatross.CommandLine.Defaults` | Pre-configured Serilog + JSON config (`WithDefaults()`) |
| `Albatross.CommandLine.Inputs` | Pre-built validated option/argument types for files and directories |
| `Albatross.CommandLine.CodeAnalysis` | Roslyn analyzers for best practices |

### Bootstrap Pattern

The skill generates a correct `Program.cs` with the required call order:

```csharp
await using var host = new CommandHost("My App Name")
    .RegisterServices(RegisterServices)
    .AddCommands()      // source-generated
    .Parse(args)
    .WithDefaults()     // optional — must come after Parse()
    .Build();
return await host.InvokeAsync();
```

### Command Definition

Every command has a params class and a handler class. The skill generates both:

```csharp
[Verb<HelloWorldHandler>("hello", Description = "Say hello")]
public record class HelloWorldParams {
    [Argument(Description = "The name to greet")]
    public required string Name { get; init; }

    // DefaultToInitializer = true is required to treat the initializer as a default value
    [Option(DefaultToInitializer = true, Description = "Number of times to repeat")]
    public int Count { get; init; } = 1;

    [Option("--shout", "-s")]
    public bool Shout { get; init; }
}

public class HelloWorldHandler : BaseHandler<HelloWorldParams> {
    private readonly ILogger<HelloWorldHandler> logger;

    public HelloWorldHandler(
        ParseResult result,
        HelloWorldParams parameters,
        ILogger<HelloWorldHandler> logger) : base(result, parameters) {
        this.logger = logger;
    }

    public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
        Writer.WriteLine(parameters.Shout
            ? $"HELLO, {parameters.Name.ToUpper()}!"
            : $"Hello, {parameters.Name}!");
        return 0;
    }
}
```

### Subcommand Hierarchies

Use spaces in the verb name to build parent→child command trees:

```csharp
[Verb("config", Description = "Configuration commands")]
public record class ConfigParams { }

[Verb<ConfigGetHandler>("config get")]
public record class ConfigGetParams {
    [Argument]
    public required string Key { get; init; }
}

[Verb<ConfigSetHandler>("config set")]
public record class ConfigSetParams {
    [Argument] public required string Key { get; init; }
    [Argument] public required string Value { get; init; }
}
```

### Reusable Option and Argument Types

Define once, use across many commands. The skill generates the correct constructor signatures required by the source generator:

```csharp
[DefaultNameAliases("--input-dir", "--in", "-i")]
public class InputDirectoryOption : Option<DirectoryInfo> {
    public InputDirectoryOption(string name, params string[] aliases) : base(name, aliases) {
        Description = "Specify an existing input directory";
        AddValidator(result => {
            if (result.GetValueForOption(this) is DirectoryInfo dir && !dir.Exists)
                result.ErrorMessage = $"Directory {dir.FullName} does not exist";
        });
    }
}
```

Use them in params classes with `[UseOption<T>]` or `[UseArgument<T>]`:

```csharp
[Verb<BackupHandler>("backup")]
public record class BackupParams {
    [UseOption<InputDirectoryOption>]
    public required DirectoryInfo Source { get; init; }

    // UseCustomName = true derives the option name from the property name
    [UseOption<InputDirectoryOption>(UseCustomName = true)]
    public required DirectoryInfo Destination { get; init; }
}
```

### Async Option Pre-processing (`OptionHandler`)

The skill knows how to generate option handlers for two scenarios:

**Validation (short-circuit on failure):**

```csharp
[OptionHandler<ApiKeyOption, ApiKeyValidator>]
public class ApiKeyOption : Option<string> { ... }

public class ApiKeyValidator : IAsyncOptionHandler<ApiKeyOption> {
    public async Task InvokeAsync(ApiKeyOption option, ParseResult result,
        CancellationToken cancellationToken) {
        if (!await auth.IsValidAsync(result.GetValue(option), cancellationToken))
            context.SetInputActionStatus(new OptionHandlerStatus(option.Name, false, "Invalid API key"));
    }
}
```

**Input transformation (string ID → rich object):**

```csharp
[OptionHandler<InstrumentOption, InstrumentOptionHandler, InstrumentSummary>]
public class InstrumentOption : Option<string> { ... }

// Property type on the params class is the *output* type, not the raw CLI type
[Verb<PriceHandler>("get-price")]
public record class GetPriceParams {
    [UseOption<InstrumentOption>]
    public required InstrumentSummary Instrument { get; init; }
}
```

Option handlers run before the command handler. If any handler marks the context as failed, the command handler is skipped.

### Built-in Inputs (`Albatross.CommandLine.Inputs`)

The skill knows to use these pre-built types instead of writing custom ones:

| Type | What it does |
|------|-------------|
| `InputFileOption` / `InputFileArgument` | Validates file exists |
| `InputDirectoryOption` / `InputDirectoryArgument` | Validates directory exists |
| `OutputFileOption` | For output file paths |
| `OutputDirectoryOption` / `OutputDirectoryArgument` | For output directory paths |
| `OutputDirectoryWithAutoCreateOption` | Auto-creates directory if missing |
| `FormatExpressionOption` | For format string inputs |
| `TimeZoneOption` | For timezone name inputs |

## Tips for Best Results

1. **Describe your intent** — tell the skill what the command should do, not just its name. Claude Code will ask clarifying questions if needed.
2. **Mention injected services** — if your handler needs services from DI, say so upfront so the skill adds the right constructor parameters.
3. **Specify subcommand relationships** — if commands share a parent, mention that so the skill sets up the hierarchy correctly.
4. **Review generated code** — the skill follows library conventions precisely, but you may want to customize descriptions or validation messages.

## Related Documentation

- [AI Agent Instructions](ai-instructions.md) — Comprehensive guidance for AI agents working with the library
- [Core Concepts](core-concepts.md) — Understanding commands, options, and arguments
- [Conventions](conventions.md) — Naming conventions and patterns
