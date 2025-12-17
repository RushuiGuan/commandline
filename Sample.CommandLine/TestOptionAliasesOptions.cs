using Albatross.CommandLine;
using System;

namespace Sample.CommandLine {
	[Verb("test option-aliases", Description = "Test creation of aliases for options")]
	public class TestOptionAliasesOptions {
		[Option("i", "int", Description = "A required integer option")]
		public required int IntValue { get; init; }

		[Option("s", "str", Description = "An optional string value")]
		public string? StringValue { get; init; }
	}
}