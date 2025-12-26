using Albatross.CommandLine;

namespace Sample.CommandLine.MutuallyExclusiveOptions {
	public record class ProjectOptions {
		[Option]
		public required int Id { get; init; }
	}
}