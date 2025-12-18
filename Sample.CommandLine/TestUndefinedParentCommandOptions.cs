using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb<DefaultCommandAction<TestUndefinedParentCommandOptions>>("p1 p2 new", Description = "The parent command project is not defined explicitly.  It will be created automatically.")]
	public class TestUndefinedParentCommandOptions {
	}
}