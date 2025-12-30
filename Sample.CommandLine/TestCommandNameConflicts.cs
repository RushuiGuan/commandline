using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestCommandNameConflictsParams>>("test command-name-conflicts-1", Description = "Command class name is derived from the Options class name by removing the 'Params' suffix and add 'Command' suffix.")]
	public record class TestCommandNameConflictsParams {
	}
	
	[Verb<DefaultAsyncCommandHandler<TestCommandNameConflictsCommandParams>>("test command-name-conflicts-2", Description = "In case of conflicts, system will append a sequence number to the conflicted command name.")]
	public record class TestCommandNameConflictsCommandParams {
	}
}