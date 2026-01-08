# Core Concepts

Things to know before you start!

## Command and Sub Commands
**Commands** are units of execution that perform specific tasks. **Commands** can have hierarchical grouping.

In the sample code below, the parent command is `codegen` and the child commands are `csharp` and `typescript`.

```csharp
[Verb("codegen", Description = "A parent command to group code generators of diff language to the same parent command")]
public record class CodeGenParams { }

[Verb<CSharpCodeGenHandler>("codegen csharp")]
public record class CSharpCodeGenParams {
	...
}
[Verb<TypeScriptCodeGenHandler>("codegen typescript")]
public record class TypeScriptCodeGenParams {
}
```

## Arguments and Options
### Arguments
- Positional parameters that don't require a name
- Order matters and is determined by property declaration order

### Options
- Named parameters
- Can appear in any order

```csharp
[Verb<CopyFileHandler>("copy")]
public record class CopyParams {
	[Argument(Description = "Source file path")]
	public required FileInfo Source { get; init; }
	
	[Argument(Description = "Destination file path")] 
	public FileInfo? Destination { get; init; }
	
	[Option("--config", "-c", Description = "Build configuration")]
	public required string Configuration { get; init; }

	[Option("--env", "-e", Description = "Build environment")]
	public required string Environment { get; init; }
}
```

## the `Parameters` Class (plural)
A `Parameters` class is a user-defined class whose properties map to a command's arguments and options. The class must be decorated with the [[Verb]](https://github.com/RushuiGuan/commandline/blob/main/Albatross.CommandLine/Annotations/VerbAttribute.cs) attribute. To define the command's inputs, its properties should be decorated with either the [[Option]](https://github.com/RushuiGuan/commandline/blob/main/Albatross.CommandLine/Annotations/OptionAttribute.cs) or [[Argument]](https://github.com/RushuiGuan/commandline/blob/main/Albatross.CommandLine/Annotations/ArgumentAttribute.cs) attribute. This class serves as the central mechanism that `Albatross.CommandLine` uses to wire up a command with its handler, arguments, and options.

```csharp
// when a class in annotated with the `VerbAttribute`, a command class will be generated
// `VerbAttribute` associates the command handler `HelloWorldHandler` and the command name "hello" with the created command
[Verb<HelloWorldHandler>("hello")]
public record class HelloWorldParams {
	// when annotated with the `ArgumentAttribute`, an argument will be generated for the command
	[Argument]
	public required string Name { get; init; }

	// when annotated with the `OptionAttribute`, an option will be generated for the command
	[Option]
	public required string Value { get; init; }

	// No generated code for this property
	public int Id { get; int; }
}
```


## Parsing and Execution
The primary function of **System.CommandLine** is cli parsing.  In terms of execution, it provides hooks for actions of the commands and their options.  The Option actions are considered as PreActions since they serve the purpose of preprocessing before the main command action is executed.  The parsing and execution are two seperated stages of the operation.  To be exact:
```
Parsing --> PreActions (Option Actions) --> Action (Command Action)
```
By default, if there are parsing errors, all PreActions will still execute but main Action will not.  Note that PreActions can short circut after a successful parsing and terminate the execution early.  It could also clear the error state of parsing and allow the main Action to execute.

*Actions cannot be defined for **System.CommandLine** Arguments.*


## Next Steps

Now that you understand the core concepts, explore:
- [Quick Start](quick-start.md) for a hands-on tutorial
- [Examples](examples.md) for practical implementations  
- [Command Customization](command-customization.md) for advanced scenarios
- [Code Generator](code-generator.md) for automatic command generation

