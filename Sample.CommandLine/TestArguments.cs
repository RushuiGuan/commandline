using Albatross.CommandLine;
using System;

namespace Sample.CommandLine {
	[Verb<DefaultCommandAction<TestArgumentsOptions>>("test arguments", Description = "A command to test arguments parsing")]
	public record class TestArgumentsOptions {
		[Argument(Description = "A required string argument")]
		public required string StringArg { get; init; }

		[Argument(Description = "A required integer argument")]
		public required int IntArg { get; init; }
		
		[Argument(Description = "An optional date argument")]
		public DateOnly? DateArg { get; init; }
	}
}