using Albatross.CommandLine;
using System;

namespace Sample.CommandLine {
	[Verb("test argument-arity", Description = "Test argument arity")]
	public class TestArgumentArityOptions {
		[Argument(ArityMin = 1, ArityMax = 10, Description = "Int collections with a count between 1 and 10")]
		public int[] IntValues { get; init; } = Array.Empty<int>();
	}
}
