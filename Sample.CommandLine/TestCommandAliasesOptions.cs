using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb("test command-aliases", Alias = ["a", "cmd-alias"], Description = "Test the use of command aliases")]
	public class TestCommandAliasesOptions {
	}
}