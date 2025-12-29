using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestNullableArguments>>("test nullable-arguments", Description = "Optional arguments should be placed after the required arguments")]
	public record class TestNullableArguments {
		[Argument(Description = "Required string value")]
		public required string RequiredStringValue { get; init; }

		[Argument(Description = "Nullable string argument")]
		public string? NullableStringArgument { get; init; }
	}
}