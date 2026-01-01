using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Inputs;
using System.IO;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestBuildInOptionsParams>>("test built-in")]
	public class TestBuildInOptionsParams {
		[UseOption<OutputDirectoryOption>(UseCustomNameAlias = true)]
		public required DirectoryInfo OutputDirectory { get; init; }
	}
}
