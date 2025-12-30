using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestNoDescriptionParams>>("test no-descriptions")]
	public record class TestNoDescriptionParams {
		[Option]
		public required int IntValue { get; init; }

		[Option]
		public required string TextValue { get; init; }
	}
}