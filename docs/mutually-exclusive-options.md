# How to Create Option Sets (Mutually Exclusive Options)

## The Old Way
[CommandLineParser](https://www.nuget.org/packages/CommandLineParser) supports mutually exclusive options set by using the `SetName` property on the `OptionAttribute`.  `System.CommandLine` doesn't have this support.  But the same functionality can be achieved by using validators.  This can be done easier using the generated commands by `Albatross.CommandLine`.

In the example below, `Id` and `Name` are mutually exclusive options.  The added validator code ensure that only one of the options can be set.  Notice that the help message does not automatically reflect the custom validation code.  So manual description on the option would be required to clearly indicate the mutually exclusive requirement.
```csharp
[Verb("mutually-exclusive-command", typeof(MutuallyExclusiveCommandHandler))]
public class MutuallyExclusiveCommandOptions {
	[Option(Required = false, Description = "Describe your option requirement here")]
	public int Id { get; set; }

	[Option(Required = false, Description = "Describe your option requirement here")]
	public string Name { get; set; } = string.Empty;
}

public partial class MutuallyExclusiveCommand : IRequireInitialization {
	public void Init() {
		this.AddValidator(result => {
			var found = result.Children.Where(x => x.Symbol == this.Option_Id || x.Symbol == this.Option_Name).ToList();
			if (found.Count == 0) {
				result.ErrorMessage = "Either Id or Name is required";
			} else if (found.Count > 1) {
				result.ErrorMessage = "Id and Name are mutually exclusive";
			};
		});
	}
}
public class MutuallyExclusiveCommandHandler : BaseHandler<MutuallyExclusiveCommandOptions> {
	public MutuallyExclusiveCommandHandler(IOptions<MutuallyExclusiveCommandOptions> options, ILogger logger) : base(options, logger) {
	}
}
```
Source file is located at [MutuallyExclusiveCommandOptions.cs](../Sample.CommandLine/Example_MutuallyExclusiveCommandOptions.cs)

## The New Way
The new way is to use sub commands.  Sub commands are natively supported by the `System.CommandLine` library.  With it, help messages will be generated correctly out of box and mutually exclusive options can be created as its own commands.  

In the example below, the parent command `search` has two sub commands `id` and `name`.  The subcommands share the same command handler `SearchCommandHandler`.  The handler can inject options from both sub commands into its constructor and decide what to do base on the command context.

Note that the parent command options `SearchOptions` doesn't need to be declared.  When omitted, the system will generate one with the `HelpCommandHandler`.

```csharp
// this declaration is not required.  The system will auto generate the parent command with a HelpCommandHandler if it is not declared
[Verb("search", typeof(HelpCommandHandler))]
public record class SearchOptions { }

[Verb("search id", typeof(SearchCommandHandler))]
public class SearchByIdOptions {
	public int Id { get; set; }
}
[Verb("search name", typeof(SearchCommandHandler))]
public class SearchByNameOptions {
	public string Name { get; set; } = string.Empty;
}
public class SearchCommandHandler : ICommandHandler {
	private readonly IOptions<SearchByIdOptions> searchByIdOptions;
	private readonly IOptions<SearchByNameOptions> searchByNameOptions;

	public SearchCommandHandler(IOptions<SearchByIdOptions> searchByIdOptions, IOptions<SearchByNameOptions> searchByNameOptions) {
		this.searchByIdOptions = searchByIdOptions;
		this.searchByNameOptions = searchByNameOptions;
	}

	public int Invoke(InvocationContext context) {
		context.Console.WriteLine($"Command: {context.ParseResult.CommandResult.Command.Name} has been invoked");
		context.Console.WriteLine($"search by id: {searchByIdOptions.Value.Id}");
		context.Console.WriteLine($"search by name: {searchByNameOptions.Value.Name}");
		return 0;
	}

	public Task<int> InvokeAsync(InvocationContext context) {
		return Task.FromResult(Invoke(context));
	}
}
```
Source file is located at [Search.cs](../Sample.CommandLine/Search.cs)

As shown in the example above, This approach results in cleaner code and it is the better way.