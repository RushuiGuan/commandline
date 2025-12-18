using Albatross.CommandLine;
using System.IO;

namespace Sample.CommandLine {
	public record class CodeGenOptions {
		[Option(Description = "Output directory for generated C# files")]
		public required DirectoryInfo OutputDirectory { get; init; }

		[Option(Description = "Project file path")]
		public required FileInfo Project { get; init; }
	}

	[Verb("csharp web-client")]
	public record class CSharpCodeGenOptions : CodeGenOptions {
		[Option(Description = "C# language version")]
		public required string LanguageVersion { get; init; }
	}

	[Verb("typescript web-client")]
	public record class TypeScriptCodeGenOptions : CodeGenOptions {
		[Option(Description = "Javascript ecma version")]
		public required string EcmaVersion { get; init; }
	}
}
