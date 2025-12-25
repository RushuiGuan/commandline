using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb<DefaultCommandHandler<TestNullableOptions>>("test nullable", Description = "A command to test nullable options")]
	public record class TestNullableOptions {
		[Option(Description = "A nullable string")]
		public string? NullableString { get; init; }

		[Option(Description = "A nullable integer")]
		public int? NullableInt { get; init; }

		[Option(Description = "A nullable array")]
		public int[]? NullableArray { get; init; }
	}
}