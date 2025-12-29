using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb("test verb-without-handler", Description = "This verb is missing handler")]
	public record class TestClassVerbWithoutHandlerParams {
	}
}