using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb("test", Description = "A series of test commands to verify the commandline functionalities")]
	public record class TestOptions {
	}
}
