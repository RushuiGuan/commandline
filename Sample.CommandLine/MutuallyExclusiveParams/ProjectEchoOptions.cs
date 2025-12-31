using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine.MutuallyExclusiveParams {
	[Verb<ExampleProjectBaseHandler>("example project echo", BaseParamsClass = typeof(ProjectParams), Description = "This demonstrates the use of mutually exclusive commands using inheritance.")]
	public record class ProjectEchoOptions : ProjectParams {
		[Option]
		public required int Echo { get; init; }
	}
}