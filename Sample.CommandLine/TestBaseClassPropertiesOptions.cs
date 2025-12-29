using Albatross.CommandLine;
using System.IO;

namespace Sample.CommandLine {
	public record class BaseOptions1 : BaseParams2 {
		[Option(Description = "Output directory for generated C# files")]
		public required DirectoryInfo OutputDirectory { get; init; }
	}

	public record class BaseParams2 {
		[Option(Description = "Project file path")]
		public required FileInfo Project { get; init; }
	}

	[Verb<DefaultAsyncCommandHandler<TestBaseClassPropertiesParams>>("test base-class-properties", Description = "When determining option property order, the code generator prioritizes properties declared on the current class over those inherited from base classes.")]
	public record class TestBaseClassPropertiesOptions : BaseParams1 {
		[Option(Description = "C# language version")]
		public required string LanguageVersion { get; init; }
	}
}