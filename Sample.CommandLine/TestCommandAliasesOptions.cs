using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestCommandAliasesOptions>>("test command-aliases", Alias = ["a", "cmd-alias"], Description = "Test the use of command aliases")]
	public record class TestCommandAliasesOptions {
	}
}