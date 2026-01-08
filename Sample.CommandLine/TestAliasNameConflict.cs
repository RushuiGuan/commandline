using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestAliasConflictParams>>("test alias-conflict")]
	public class TestAliasConflictParams {
		[Option("n", "n1")]
		public string? Name1 { get; set; }

		[Option("n", "n2")]
		public string? Name2 { get; set; }

		[Option("--name1", "n3")]
		public string? Name3 { get; set; }
	}
}
