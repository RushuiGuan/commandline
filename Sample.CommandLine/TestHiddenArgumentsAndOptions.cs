using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestHiddenPropertiesParams>>("test hidden", Description = "Test hidden arguments and parameters: --hidden-string-value, --hidden-int-value")]
	public record class TestHiddenPropertiesParams {
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