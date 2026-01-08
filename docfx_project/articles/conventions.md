# Terminologies and Conventions

## Terminology
- **Command** - A unit of execution.  A CLI can contain may commands
- **Option** - Named parameter
- **Argument** - Positioned parameter
- **Parameters Class** - A class that contains all the parameters of a command.  It is also used as a centralized location to configure the command and its handler using annotation.


## Parameters Class Naming Convention

Parameter classes should be created with the postfix `Params`, e.g., `BackupParams`.  The generated command class would have the name of `BackupCommand`.  The convention of the command class name is to remove postfix `Params` if exists and append `Command` to the class name.

```csharp
[Verb<BackupCommandHandler>("backup", Description = "Back up data")]
public class BackupParams {
}

// generated command class
public sealed partial class BackupCommand {
	...
}
```
## Command Handler Naming Convention
There is no strict requirement on the naming of command handlers. Common patterns include:
- Postfixing with `CommandHandler` or `Handler` (e.g., `BackupCommandHandler` or `BackupHandler`)
- Simple names matching the command (e.g., `Backup`)

`System.CommandLine` use the word `Action` to name the delegates associated with the Command or Options.  `Albatross.CommandLine` follow the convention and name all delegates directly attached with postfix of `Action` as well.  Those delegates are the internals of `Albatross.CommandLine` and should not be set by the user directly.  To have a distinction, user defined delegates are called `Handlers` or `Command Handlers` in the documentation.

## Argument and Option Naming Convention
The arguments and options names are created from property name by converting to lower cases and kebaberized.  Additionally option names are prefixed with `--`.  There are situation where this setup could lead to duplicate names.  But that scenario is very bad practice and will not be addressed by codegen.  Instead a code analysis warning would be created for this in the future.

```csharp
[Verb<BackupCommandHandler>("backup", Description = "Back up data")]
public class BackupParams {
	// actual argument name: input-folder
	[Argument]
	public required DirectoryInfo InputFolder { get; init; }

	// actual option name: --output-folder
	[Option]
	public required DirectoryInfo OutputFolder { get; init; }

	// this is legal in C# but poorly named.  It will also break the generated code.  A code analysis warning will be created for this scenario in the next release.
	[Option]
	public required DirectoryInfo outputFolder { get; init; }
}
```

## Default Requirement of Option
The nullability of a property is used to determine if the property option is required.  The `Required` property can also be set directly on the `Option` attribute to change the behavior.  The C# `required` keyword is not used since it is not available on all dotnet versions.

*Exceptions to the Rule*
* Boolean flag
* Collection as Input
* Property with an initializer and the `Option` attribute with `DefaultToInitializer` = true

```csharp
[Verb("example")]
public record class ExampleParams {
    // Required - non-nullable reference type
	[Option]
    public required string Name { get; init; }
    
    // Required - non-nullable value type
	[Option]
    public required int Value { get; init; }
    
    // Optional - nullable reference type
	[Option]
    public string? Description { get; init; }
    
    // Optional - nullable value type
	[Option]
    public int? OptionalValue { get; init; }

	// Optional - boolean flag
	[Option]
	public bool Flag { get; init; }

	// optional - collection
	[Option]
	public int[] Array { get; init; }

	// optional - has an initializer and DefaultToInitializer is set to true
	[Option(DefaultToInitializer = true)]
	public string Value { get; init; } = "Value";
    
    // Override default behavior with Required attribute
    [Option(Required = false)]
    public int NonRequiredValue { get; init; }
    
    [Option(Required = true)]
    public string? RequiredNullableValue { get; init; }
}
```

## Default Arity of Arguments
Similar to Options, default arity of arguments are determined by its property nullability and type.  `ArityMin` and `ArityMax` property from the `Argument` attribute can be used to override the behavior.

* Property is a collection → `ArityMin = 0, ArityMax = int.MaxValue`
* Property is nullable → `ArityMin = 0, ArityMax = 1`
* Property is not nullable → `ArityMin = 1, ArityMax = 1`
	- Property has an initializer and `DefaultToInitializer` as true → `ArityMin = 1, ArityMax = 1`

```csharp
[Verb<BackupCommandHandler>("backup", Description = "Back up data")]
public class BackupParams {
	// ArityMin = 1, ArityMax = 1
	[Argument]
	public required DirectoryInfo InputFolder { get; init; }

	// ArityMin = 0, ArityMax = 1
	[Argument]
	public DirectoryInfo? OutputFolder { get; init; }

	// ArityMin = 0, ArityMax = 1
	[Argument(DefaultToInitializer = true)]
	public int IntValue { get; init; } = 10

	// ArityMin = 0, ArityMax = 100_000
	[Argument]
	public int[] Array { get; init; } 

	// this one is override manually
	[Argument(ArityMin = 10, ArityMax = 12)]
	public int[] Array2 { get; init; } 
}
```

## Option Alias Prefix Convention
Aliases are automatically prefixed according to these rules:
- Single character aliases get single dash prefix: `"f"` → `"-f"`
- Multi-character aliases get double dash prefix: `"file"` → `"--file"`
- No prefix is added if dashes are already present: `"-file-name"` remains `"-file-name"`

## Generated Property Names Convention
Within the command class, option and argument properties are generated with specific prefixes:
- Option properties: `Option_PropertyName` (e.g., `Option_FileName`)
- Argument properties: `Argument_PropertyName` (e.g., `Argument_Name`)

```csharp
[Verb("backup")]
public class BackupParams {
	[Option]
	public required string FileName { get; init ; }

	[Argument]
	public required string Source { get; init ; }
}
// generated command
public sealed partial class BackupCommand : Command {
	public BackupCommand() : base("backup")  {
		this.Option_FileName = new System.CommandLine.Option<string>("--file-name") {
			Required = true,
		};
		this.Add(this.Option_FileName);
		this.Argument_Source = new System.CommandLine.Argument<string>("source") {
			Arity = new ArgumentArity(1, 1),
		};
		this.Add(this.Argument_Source);
		this.Initialize();
	}
	public System.CommandLine.Option<string> Option_FileName {
		get;
	}
	public System.CommandLine.Argument<string> Argument_Source {
		get;
	}
	partial void Initialize();
}
```