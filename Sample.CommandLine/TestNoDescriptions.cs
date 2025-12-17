using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb("test no-descriptions")]
	public record class TestNoDescriptionOptions{
		[Option]
		public required int IntValue { get; init; }

		[Option]
		public required string TextValue { get; init; }
	}
}
