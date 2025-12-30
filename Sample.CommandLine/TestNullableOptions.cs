using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestNullableParams>>("test nullable", Description = "A command to test nullable parameters")]
	public record class TestNullableParams {
		[Option(Description = "A nullable string")]
		public string? NullableString { get; init; }

		[Option(Description = "A nullable integer")]
		public int? NullableInt { get; init; }

		[Option(Description = "A nullable array")]
		public int[]? NullableArray { get; init; }
	}
}