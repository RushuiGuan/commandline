using Albatross.CommandLine;

namespace Sample.CommandLine.MutuallyExclusiveOptions {
	[Verb<ExampleProjectBaseHandler>("example project fubar", UseBaseOptionsClass = typeof(ProjectOptions), Description = "This demonstrates the use of mutually exclusive commands using inheritance.")]
	public record class ProjectFubarOptions : ProjectOptions {
		[Option]
		public required int Fubar { get; init; }
	}
}