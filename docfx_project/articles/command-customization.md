# Customizing Commands with Partial Classes

Albatross.CommandLine generates partial command classes that can be extended to add custom behavior, validation, and configuration. This allows developers to leverage both code generation benefits and custom logic.

## Find the Generated Command Class
The name of the command class is constructed by:
1. The name of `Params` class
2. Remove 'Params' at the end if exists
2. Append 'Command' at the end

Therefore: `HelloWorldParams` --> `HelloWorldCommand`

When a parameters class is annotated with multiple verbs, each verb will have its own command class.  To distinguish them, the generated command classes will be postfixed with a number.

```csharp
[Verb("hello sun")]
[Verb("hello moon")]
public class HelloWorldParams {
    ...
}
```
The `Verb` above will generate 2 command classes:
```csharp
public sealed partial class HelloWorldCommand : base("hello sun") {
    ...
}
public sealed partial class HelloWorldCommand1 : base("hello moon") {
    ...
}
```
To view the generated files, put the following tags in the project file, build the project and they will be found in: `[project root]\obj\generated`.
```xml
<PropertyGroup>
	<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
	<CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

## Customize the Command By Implementing the `Initialize` method
The `Initialize` partial method is called as the last statement of the constructor of the generated command.  Here's a simple example.

```csharp
[Verb<TestCustomizedParams>("test customized", Description = "Commands can be customized by extending its partial class")]
public record class TestCustomizedParams {
    [Option("d")]
    public required string Description { get; init; }
}

// generated command
public sealed partial class TestCustomizedCommand : Command {
	public TestCustomizedCommand() : base("customized", "Commands can be customized by extending its partial class")  {
		this.Option_Description = new System.CommandLine.Option<string>("--description", "-d") {
			Required = true,
		};
		this.Add(this.Option_Description);
		this.Initialize();
	}
	public System.CommandLine.Option<string> Option_Description {
		get;
	}
	partial void Initialize();
}

// Customize the command using the Initialize method
public partial class TestCustomizedCommand {
    partial void Initialize() {
        this.Option_Description.Validators.Add(r => {
            var text = r.GetRequiredValue(this.Option_Description);
            if (text.Length < 3) {
                r.AddError("Description must be at least 3 characters long.");
            }
        });
    }
}
```
Note that all parameter symbols are generated as properties of the command.  They can be used directly by the customization code.  See the [conventions](./conventions.md#generated-property-names-convention) page for property naming conventions.