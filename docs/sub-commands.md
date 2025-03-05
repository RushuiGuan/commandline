# Sub Commands

Sub command capability is one of the differentiators between `System.CommandLine` and other parsers.  To create sub commands using `Albatross`, use a `VerbAttribute` with a space delimited name.  


```csharp
[Verb("search id")]
public class MySubOptions { 
}
```
The example above tells the system to create a command `id` with the parent command `search`.  The parent command options can be declared explicitly.  If missing, the system will create one automatically.  A good use case for the sub command is to create [Mutually Exclusive Options](./mutually-exclusive-options.md#the-new-way).

Here is a few more code example for sub commands [[source](../Sample.CommandLine/Example_SubCommands.cs)]:

1. parent1 has two subcommands sub1 and sub2,  parent1 itself is not declared and will be generated with a default HelpCommandHandler.  Sub1 and sub2 are declared without a command handler, therefore the system assigns the [DefaultCommandHandler](../Albatross.CommandLine/DefaultCommandHandler.cs).
	```csharp
	[Verb("parent1 sub1")]
	public class MyParent1Sub1Options { }
	
	[Verb("parent1 sub2")]
	public class MyParnet1Sub2Options { }
	```
1. parent2 has two subcommands sub1 and sub2,  parent2 itself is declared with a custom handler.  All three commands should work independently.  Notice that the sub commands names are only unique within the same parent.
	```csharp
	[Verb("parent2 sub1")]
	public class MyParent2Sub1Options { }
	
	[Verb("parent2 sub2")]
	public class MyParent2Sub2Options { }

	[Verb("parent2", typeof(MyParent2Handler))]
	public class MyParent2Options { }
	public class MyParent2Handler : BaseHandler<MyParent2Options> {
		public MyParent2Handler(IOptions<MyParent2Options> options) : base(options) {
		}
	}
	```
1. parent3 has a subcommand sub1 and they share the same handler.  The sub command options inherits from the parent options.  In this situation, the sub command in a sense extends the parent command with additional options
	```csharp
	[Verb("parent3", typeof(MyParent3Handler))]
	public class MyParent3Options {
		public int Id { get; set; }
	}
	
	[Verb("parent3 sub1", typeof(MyParent3Handler))]
	public class MyParent3Sub1Options : MyParent3Options {
		public string Name { get; set; } = string.Empty;
	}

	public class MyParent3Handler : BaseHandler<MyParent3Sub1Options> {
		public MyParent3Handler(IOptions<MyParent3Sub1Options> options) : base(options) { }
		public override int Invoke(InvocationContext context) {
			// remove base.Invoke and do some work here
			base.Invoke(context);
			return 0;
		}
	}
	```