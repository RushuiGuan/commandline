using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestUndefinedParentCommandParams>>("p1 p2 new", Description = "The parent command project is not defined explicitly.  It will be created automatically.")]
	public record class TestUndefinedParentCommandParams {
	}
}