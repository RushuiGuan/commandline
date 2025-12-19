using Albatross.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	public record class SharedBaseOptions {
		[Option(Description = "Output directory for generated C# files")]
		public required DirectoryInfo OutputDirectory { get; init; }

		[Option(Description = "Project file path")]
		public required FileInfo Project { get; init; }
	}

	[Verb("test base-class-properties")]
	public record class TestBaseClassPropertiesOptions : SharedBaseOptions {
		[Option(Description = "C# language version")]
		public required string LanguageVersion { get; init; }
	}
}
