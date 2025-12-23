# About

A companion code generator used by the [Albatross.CommandLine](../Albatross.CommandLine) library. It supports generation
of arguments, commands, subcommands and options with dependency injection.

## Quick Start

As a development dependency of [Albatross.CommandLine](../Albatross.CommandLine) library, codegen will be referenced
automatically as a PrivateAssets when the reference for [Albatross.CommandLine](../Albatross.CommandLine) is added to a
project. The code generator looks for options classes those are annotated with
the [Albatross.CommandLine.VerbAttribute](../Albatross.CommandLine/VerbAttribute.cs) and generate the appropriate
command classes. In the example below, the class `TestOptions`, `TestCommandAction` and the first `TestCommand` class
are created manually and the second `TestCommand` class is generated.

* The command is created as a partial class which allows user to add additional functionalities. To customize a command,
  create a partial command class of the same name and implement the partial method `Initialize`.  The `Initialize` method is invoked at the end of the constructor.
* Nullable property are declared as optional and vice vesa. However, requirement can be overwritten using
  the [Albatross.CommandLine.OptionAttribute](../Albatross.CommandLine/OptionAttribute.cs) as shown in the `Value`
  property in the example.
* Option alias can be created using
  the [Albatross.CommandLine.OptionAttribute](../Albatross.CommandLine/OptionAttribute.cs). Aliases are prefixed with a
  single dash ('-') if it is a single character otherwise double dash ('--') is used.

```csharp
[Verb("test", typeof(TestCommandAction), Description = "A test command")]
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
public class TestCommandAction : ICommandAction {
	...
}
public partial class TestCommand  {
	// this method will be call right after object construction
	partial void Initialize() {
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
		this.Add(Option_Name);
		this.Option_Description = new Option<string?>("--description", null);
		this.Add(Option_Description);
		this.Option_Value = new Option<int>("--value", "An integer value");
		Option_Value.AddAlias("-v");
		this.Add(Option_Value);
	}

	public Option<string> Option_Name { get; }
	public Option<string?> Option_Description { get; }
	public Option<int> Option_Value { get; }
}
```

The second part of the code generator will create the service registration and the options registration code. The
`RegisterCommands` method should be invoked by service registration.  `AddCommands` method is
part of the bootstrap code in `program.cs` file. See [Albatross.CommandLine](../Albatross.CommandLine/README.md) for
details.

```csharp
public static class RegistrationExtensions
	{
		public static IServiceCollection RegisterCommands(this IServiceCollection services) {
			services.AddKeyedScoped<ICommandAction, Sample.CommandLine.HelloWorldCommandAction>("hello");
			services.AddScoped<Sample.CommandLine.HelloWorldOptions>(provider => {
				var result = provider.GetRequiredService<ParseResult>();
				var options = new Sample.CommandLine.HelloWorldOptions() {
					Argument1 = result.GetRequiredValue<string>("argument1"),
					Argument2 = result.GetValue<string?>("argument2"),
					Name = result.GetRequiredValue<string>("--name"),
					Value = result.GetRequiredValue<int>("--value"),
					NumericValue = result.GetValue<decimal>("--numeric-value"),
					Date = result.GetRequiredValue<System.DateOnly>("--date"),
				};
				return options;
			});
			return services;
		}

		public static CommandHost AddCommands(this CommandHost host) {
			host.CommandBuilder.Add<HelloWorldCommand>("hello");
			return host;
		}
	}
```

## Troubleshooting
In visual studio, `EmitCompilerGeneratedFiles` property can be added to view the generated code.
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <EmitAlbatrossCodeGenDebugFile>true</EmitAlbatrossCodeGenDebugFile>
    </PropertyGroup>
</Project>
```
For Rider, generated code can be found under
`Project -> Dependencies -> .NET (Version) -> Source Generators`. 