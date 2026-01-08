using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine.MutuallyExclusiveParams {
	[Verb<ProjectHandler>("project fubar", BaseParamsClass = typeof(ProjectParams), Description = "This demonstrates the use of mutually exclusive commands using inheritance.")]
	public record class ProjectFubarParams : ProjectParams {
		[Option]
		public required int Fubar { get; init; }
	}
}