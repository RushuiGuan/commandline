using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Sample.CommandLine {
	// parent1 has two subcommands sub1 and sub2,  if the parent itself is not declared, it will
	// be generated with a default HelpCommandHandler
	[Verb("parent1 sub1", Description = "This demonstrate the creation of a sub command.  The verb should be composed of \"[parent name] [sub command name]\"")]
	public class MyParent1Sub1Options { }
	[Verb("parent1 sub2", Description = "If the parent command option is not declared, it will be generated automatically")]
	public class MyParent1Sub2Options { }

	[Verb("parent2 sub1", Description = "Notice that uniqueness is only required for the full command name")]
	public class MyParent2Sub1Options { }
	[Verb("parent2 sub2", Description = "both parent1 and parent2 have the sub commands of the same name")]
	public class MyParent2Sub2Options { }

	[Verb("parent1", Description = "parent1 doesn't have any handler or parameter, its options can be omitted")]
	[Verb("parent2", Description = "However, without the options, there is no help messages.  So the same 'ParentOptions' class are being used for the purpose of setting descriptions")]
	public class ParentCommandOptions { }


	// parent3 has a subcommand sub1 and they share the same handler.  The sub command options
	// inherits from the parent options.  In this situation, the sub command in a sense extends
	// the parent command with additional options
	[Verb("parent3", typeof(MyParent3Handler), Description ="This command shares the same handler and option class as its sub command")]
	public class MyParent3Options {
		public int Id { get; set; }
	}
	[Verb("parent3 sub1", typeof(MyParent3Handler), Description ="This command shares the same handler and base option class as its parent command")]
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
}
