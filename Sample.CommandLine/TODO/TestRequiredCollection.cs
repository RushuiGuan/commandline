using Albatross.CommandLine;

namespace Sample.CommandLine {
	[Verb("test-required-collections")]
	public record class TestRequiredCollectionOptions {
		[Option(Description = "The id parameter should have a minimum of 1 value")]
		public int[] Id { get; set; } = [];
	}
}
