using Albatross.CommandLine;
using System;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestHiddenPropertiesOptions>>("test hidden", Description = "Test hidden arguments and options: --hidden-string-value, --hidden-int-value")]
	public record class TestHiddenPropertiesOptions {
		[Argument(Description = "A string value")]
		public required string StringValue { get; init; }

		[Argument(Description = "A hidden argument value", Hidden = true)]
		public string? HiddenStringValue { get; init; }

		[Option(Description = "A int value")]
		public required int IntValue { get; init; }

		[Option(Description = "A hidden int value", Hidden = true)]
		public int? HiddenIntValue { get; init; }

		/// <summary>
		/// This value should be ignored since it is not annotated with Argument or Option
		/// </summary>
		public DateOnly IgnoredValue { get; init; }
	}
}