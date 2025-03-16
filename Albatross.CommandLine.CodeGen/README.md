# About

A companion code generator used by the [Albatross.CommandLine](../Albatross.CommandLine) library. It supports generation
of arguments, commands, subcommands and options with dependency injection.

## Quick Start

As a development dependency of [Albatross.CommandLine](../Albatross.CommandLine) library, codegen will be referenced
automatically as a PrivateAssets when the reference for [Albatross.CommandLine](../Albatross.CommandLine) is added to a
project. The code generator looks for options classes those are annotated with
the [Albatross.CommandLine.VerbAttribute](../Albatross.CommandLine/VerbAttribute.cs) and generate the appropriate
command classes. In the example below, the class `TestOptions`, `TestCommandHandler` and the first `TestCommand` class
are created manually and the second `TestCommand` class is generated.

* The command is created as a partial class which allows user to add additional functionalities. To customize a command,
  create a partial command class of the same name with the
  interface [Albatross.CommandLine.IRequireInitialization](../Albatross.CommandLine/IRequireInitialization.cs).
* Nullable property are declared as optional and vice vesa. However, requirement can be overwritten using
  the [Albatross.CommandLine.OptionAttribute](../Albatross.CommandLine/OptionAttribute.cs) as shown in the `Value`
  property in the example.
* Option alias can be created using
  the [Albatross.CommandLine.OptionAttribute](../Albatross.CommandLine/OptionAttribute.cs). Aliases are prefixed with a
  single dash ('-') if the dash has not been prefixed already.

```csharp
[Verb("test", typeof(TestCommandHandler), Description = "A test command")]
public record class TestOptions {
	// required since its type is not nullable
	public string Name { get; set; } = string.Empty;
	// not required since its type is nullable
	public string? Description { get; set; }
	// not required since the default behavior is overwritten by the Option attribute
	[Option("v", "value", Required = false, Description = "An integer value")]
	public int Value { get; set; }
}
// implement your command handler logic here
// optionally use BaseHandler<OptionType> class as the base class
public class TestCommandHandler : ICommandHandler {
	...
}
public partial class TestCommand : IRequireInitialization {
	// this method will be call right after object construction
	public void Init() {
		// customize your command here
		...
	}
}
// generated code.  Option properties are created with the Prefix of `Option_`
public sealed partial class TestCommand : Command {
	public TestCommand() : base("test", "A test command") {
		this.Option_Name = new Option<string>("--name", null) {
			IsRequired = true
		};
		this.AddOption(Option_Name);
		this.Option_Description = new Option<string?>("--description", null);
		this.AddOption(Option_Description);
		this.Option_Value = new Option<int>("--value", "An integer value");
		Option_Value.AddAlias("-v");
		this.AddOption(Option_Value);
	}

	public Option<string> Option_Name { get; }
	public Option<string?> Option_Description { get; }
	public Option<int> Option_Value { get; }
}
```

The second part of the code generator will create the service registration and option binding code. The
`RegisterCommands` method should be invoked by service registration code in the Setup class.  `AddCommands` method is
part of the bootstrap code in `program.cs` file. See [Albatross.CommandLine](../Albatross.CommandLine/README.md) for
details.

```csharp
public static class RegistrationExtensions
	{
		public static IServiceCollection RegisterCommands(this IServiceCollection services) {
			services.AddKeyedScoped<ICommandHandler, TestCommandHandler>("test");
			services.AddOptions<TestOptions>().BindCommandLine();
			return services;
		}

		public static Setup AddCommands(this Setup setup) {
			setup.AddCommand<TestCommand>();
			return setup;
		}
	}
```

## Troubleshooting

In visual studio, `EmitCompilerGeneratedFiles` property can be added to view the generated code but Visual Studio
doesn't work reliably. As an alternative, the code generator will output the debug file
`albatross.commandline.codegen.txt` if the following properties are set in the project file.

```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <EmitAlbatrossCodeGenDebugFile>true</EmitAlbatrossCodeGenDebugFile>
    </PropertyGroup>
    <ItemGroup>
        <CompilerVisibleProperty Include="EmitAlbatrossCodeGenDebugFile"/>
    </ItemGroup>
</Project>
```

Rider can display the roslyn generated file reliably. They can be found under
`Project -> Dependencies -> .NET (Version) -> Source Generators`. 