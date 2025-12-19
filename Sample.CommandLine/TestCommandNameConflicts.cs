using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb<DefaultCommandAction<TestCommandNameConflictsOptions>>("test command-name-conflicts-1", Description = "Command class name is derived from the Options class name by removing the 'Options' suffix and add 'Command' suffix.")]
	public record class TestCommandNameConflictsOptions {
	}
	
	[Verb<DefaultCommandAction<TestCommandNameConflictsCommandOptions>>("test command-name-conflicts-2", Description = "In case of conflicts, system will append a sequence number to the conflicted command name.")]
	public record class TestCommandNameConflictsCommandOptions {
	}
}