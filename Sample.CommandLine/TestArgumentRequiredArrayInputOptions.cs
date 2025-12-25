using Albatross.CommandLine;
using System;

namespace Sample.CommandLine {
	[Verb<DefaultCommandHandler<TestArgumentRequiredArrayInputOptions>>("test argument-required-array", Description = "Test argument required arity for multiple values")]
	public record class TestArgumentRequiredArrayInputOptions {
		[Argument(ArityMin = 1, ArityMax = 10, Description = "Int collections with a count between 1 and 10")]
		public int[] IntValues { get; init; } = Array.Empty<int>();
	}

	[Verb<DefaultCommandHandler<TestArgumentOptionalArrayInputOptions>>("test argument-optional-array", Description = "Test argument optional arity for multiple values")]
	public record class TestArgumentOptionalArrayInputOptions {
		[Argument(ArityMin = 0, ArityMax = 10, Description = "Int collections with a count between 0 and 10")]
		public int[] IntValues { get; init; } = Array.Empty<int>();
	}
}
