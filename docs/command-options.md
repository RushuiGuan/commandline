# Command Options
Command Options class is the entry point to create a new command.  When annotated with the [VerbAttribute](../Albatross.CommandLine/VerbAttribute.cs), code generator will pick it up and generate the appropriate command code.  The [VerbAttribute](../Albatross.CommandLine/VerbAttribute.cs) also links the command handler type with the verb using its second parameter.  Note that the command handler is associated with the verb not the options class.  Multiple command handlers can shared the same options class and multiple verbs can share the same command handler.

```csharp
// declared options class
[Verb("hello", typeof(HelloWorldCommandHandler), Description = "HelloWorld command")]
public record class HelloWorldOptions {
	[Argument(Description = "Give me a name")]
	public string Name { get; set; } = string.Empty;

	[Option("d", Description = "Give me an optional date")]
	public DateOnly? Date { get; set; }

	[Option("x", Description = "Give me a number", DefaultToInitializer = true)]
	public int Number { get; set; } = 100;
}

// generated command class
public sealed partial class HelloWorldCommand : Command
{
	public HelloWorldCommand() : base("hello", "HelloWorld command")
	{
		this.Option_Name = new Option<string>("--name", "Give me a name")
		{
			IsRequired = true
		};
		Option_Name.AddAlias("-n");
		this.AddOption(Option_Name);
		this.Option_Date = new Option<System.DateOnly?>("--date", "Give me an optional date");
		Option_Date.AddAlias("-d");
		this.AddOption(Option_Date);
		this.Option_Number = new Option<int>("--number", "Give me a number");
		Option_Number.AddAlias("-x");
		Option_Number.SetDefaultValue(100);
		this.AddOption(Option_Number);
	}
	public Option<string> Option_Name { get; }
	public Option<System.DateOnly?> Option_Date { get; }
	public Option<int> Option_Number { get; }
}
```