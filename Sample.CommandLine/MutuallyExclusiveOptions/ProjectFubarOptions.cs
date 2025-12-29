using Albatross.CommandLine;

namespace Sample.CommandLine.MutuallyExclusiveParams {
	[Verb<ExampleProjectBaseHandler>("example project fubar", UseBaseOptionsClass = typeof(ProjectParams), Description = "This demonstrates the use of mutually exclusive commands using inheritance.")]
	public record class ProjectFubarOptions : ProjectParams {
		[Option]
		public required int Fubar { get; init; }
	}
}