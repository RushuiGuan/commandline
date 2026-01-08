using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestCommandNameConflictsParams>>("test command-name-conflicts-1", Description = "Conflicts occur when a params class have multiple verbs")]
	[Verb<DefaultAsyncCommandHandler<TestCommandNameConflictsParams>>("test command-name-conflicts-2", Description = "In case of conflicts, system will append a sequence number to the conflicted command name.")]
	public record class TestCommandNameConflictsParams {
	}
}