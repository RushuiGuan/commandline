using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Albatross.CommandLine.Inputs;
using System.IO;

namespace Sample.CommandLine {
	[Verb<DefaultAsyncCommandHandler<TestReusableParams>>("test reusable")]
	public class TestReusableParams {
		[UseOption<OutputDirectoryOption>(UseCustomName = true)]
		public required DirectoryInfo OutputDirectory { get; init; }
	}
}
