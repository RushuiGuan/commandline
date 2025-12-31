using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.BuiltIn;
using System.IO;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestBuildInOptionsParams>>("test built-in")]
	public class TestBuildInOptionsParams {
		[UseOption<OutputDirectoryOption>(UseDefaultNameAlias = true)]
		public required DirectoryInfo OutputDirectory { get; init; }
	}
}
