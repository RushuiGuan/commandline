using Albatross.CommandLine;

namespace Sample.CommandLine.MutuallyExclusiveOptions {
	[Verb<ExampleProjectBaseHandler>("example project echo", UseBaseOptionsClass = typeof(ProjectOptions), Description = "This demonstrates the use of mutually exclusive commands using inheritance.")]
	public record class ProjectEchoOptions : ProjectOptions {
		[Option]
		public required int Echo { get; init; }
	}
}