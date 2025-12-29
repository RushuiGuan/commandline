using Albatross.CommandLine;

namespace Sample.CommandLine.MutuallyExclusiveParams {
	public record class ProjectParams {
		[Option]
		public required int Id { get; init; }
	}
}