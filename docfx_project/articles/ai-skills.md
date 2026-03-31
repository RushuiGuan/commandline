# Claude Code Skills for Albatross.CommandLine

This repository includes a [Claude Code](https://docs.anthropic.com/en/docs/claude-code) skill that helps you build CLI applications using the Albatross.CommandLine library.

## What is a Claude Code Skill?

A skill is a set of instructions that teaches Claude Code how to perform specific tasks. The `albatross-commandline` skill provides templates and guidance for:

- Creating new commands with handlers
- Bootstrapping new CLI projects
- Adding reusable option types
- Configuring file-based Serilog logging

## Installation

### Option 1: Download from GitHub

1. Create the skills directory in your project (if it doesn't exist):

```bash
mkdir -p .claude/skills
```

2. Download the skill folder:

```bash
# Using curl
curl -L https://github.com/RushuiGuan/commandline/archive/main.tar.gz | tar -xz --strip-components=2 -C .claude/skills commandline-main/skills/albatross-commandline


### Option 2: Manual Download

1. Navigate to [skills/albatross-commandline](https://github.com/RushuiGuan/commandline/tree/main/skills/albatross-commandline) on GitHub
2. Download `SKILL.md`
3. Place it in your project at `.claude/skills/albatross-commandline/SKILL.md`


## Directory Structure

After installation, your project should have:

```
your-project/
├── .claude/
│   └── skills/
│       └── albatross-commandline/
│           └── SKILL.md
├── src/
│   └── ...
└── ...
```

## Usage

Once installed, invoke the skill in Claude Code by typing a slash command:

```
/albatross-commandline <action> [arguments]
```

### Available Actions

| Action | Description |
|--------|-------------|
| `new-verb <name> [description]` | Create a new command with handler |
| `new-project <name>` | Bootstrap a new CLI project |
| `add-reusable-option <name>` | Create a reusable option type |
| `config-logging` | Configure file-based Serilog logging |

## Detailed Action Examples

### Action: `new-verb`

Creates a new command with its parameters class and handler.

**Invoke:**
```
/albatross-commandline new-verb backup "Backup files to a destination"
```

**What happens:**
1. Claude Code asks what options and arguments your command needs
2. You describe your requirements (e.g., "source directory, destination directory, and an optional --overwrite flag")
3. Claude Code generates the code:

```csharp
[Verb<BackupHandler>("backup", Description = "Backup files to a destination")]
public record class BackupParams {
    [Argument(Description = "Source directory")]
    public required DirectoryInfo Source { get; init; }

    [Argument(Description = "Destination directory")]
    public required DirectoryInfo Destination { get; init; }

    [Option(Description = "Overwrite existing files")]
    public bool Overwrite { get; init; }
}

public class BackupHandler : BaseHandler<BackupParams> {
    public BackupHandler(ParseResult result, BackupParams parameters)
        : base(result, parameters) { }

    public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
        // Implementation here
        return 0;
    }
}
```

**Subcommands:** Use spaces in the name for hierarchical commands:
```
/albatross-commandline new-verb "config set" "Set a configuration value"
```

### Action: `new-project`

Bootstraps a new CLI project with all required files.

**Invoke:**
```
/albatross-commandline new-project MyTool
```

**What Claude Code generates:**

1. **MyTool.csproj** - Project file with package references:
```xml
<PackageReference Include="Albatross.CommandLine" Version="*" />
<PackageReference Include="Albatross.CommandLine.Defaults" Version="*" />
```

2. **Program.cs** - Entry point with CommandHost:
```csharp
await using var host = new CommandHost("MyTool")
    .RegisterServices(RegisterServices)
    .AddCommands()
    .Parse(args)
    .WithDefaults()
    .Build();
return await host.InvokeAsync();
```

3. **HelloWorld.cs** - Sample command to get started

### Action: `add-reusable-option`

Creates a reusable option type with validation that can be shared across commands.

**Invoke:**
```
/albatross-commandline add-reusable-option InputFile
```

**What happens:**
1. Claude Code asks about the option type and validation rules
2. Generates a reusable option class:

```csharp
[DefaultNameAliases("--input", "-i")]
public class InputFileOption : Option<FileInfo> {
    public InputFileOption(string name, params string[] aliases)
        : base(name, aliases) {
        Description = "Input file path";
        this.AddValidator(result => {
            var file = result.GetValueForOption(this);
            if (file != null && !file.Exists)
                result.ErrorMessage = $"File not found: {file.FullName}";
        });
    }
}
```

**Usage in commands:**
```csharp
[Verb<ProcessHandler>("process")]
public record class ProcessParams {
    [UseOption<InputFileOption>]
    public required FileInfo Input { get; init; }
}
```

### Action: `config-logging`

Configures Serilog for file-based logging without console output.

**Invoke:**
```
/albatross-commandline config-logging
```

**What Claude Code generates:**

1. **Updated Program.cs** - Using `SetupSerilog` without console:
```csharp
.WithConfig()
.ConfigureHost(builder => {
    builder.UseSerilog();
    builder.ConfigureLogging((context, logging) => {
        var setupSerilog = new SetupSerilog();
        setupSerilog.UseConfigFile(EnvironmentSetting.DOTNET_ENVIRONMENT.Value, null, null, true);
        // No UseConsole() - keeps console free for app output
        setupSerilog.Create();
    });
})
```

2. **serilog.json** - File sink configuration:
```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": {
      "File": {
        "Name": "File",
        "Args": {
          "path": "./logs/app.log",
          "rollingInterval": "Day"
        }
      }
    }
  }
}
```

## Tips for Best Results

1. **Be specific** - Describe your requirements clearly when Claude Code asks questions
2. **Use descriptive names** - Command names like `sync-files` are better than `sf`
3. **Mention dependencies** - If your handler needs services, mention them so Claude Code adds constructor injection
4. **Review generated code** - Claude Code follows conventions but you may want to customize

## Related Documentation

- [AI Agent Instructions](ai-instructions.md) - Comprehensive guidance for AI agents working with the library
- [Core Concepts](core-concepts.md) - Understanding commands, options, and arguments
- [Conventions](conventions.md) - Naming conventions and patterns
