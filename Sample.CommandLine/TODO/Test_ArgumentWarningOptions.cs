using Albatross.CommandLine;
using System.CommandLine;

namespace Sample.CommandLine {
	[Verb("test-argument-warning", Description = "test out argument warning")]
	public record class ArgumentWarningOptions {
		[Argument(Order = 3)]
		public string Name { get; set; } = string.Empty;

		[Argument]
		public string[] Description { get; set; } = [];

		[Argument]
		public int[] Values { get; set; } = [];
	}
}
