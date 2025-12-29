using Albatross.CommandLine;
using System;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestArrayInputParams>>("test argument-required-array", Description = "Test argument required arity for multiple values")]
	public record class TestArrayInputParams {
		[Argument(ArityMin = 1, ArityMax = 10, Description = "Int collections with a count between 1 and 10")]
		public int[] RequiredIntValues { get; init; } = Array.Empty<int>();
		
		[Argument(ArityMin = 0, ArityMax = 10, Description = "Int collections with a count between 0 and 10")]
		public string[] OptionalStringValues { get; init; } = Array.Empty<string>();
	}
}
