using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb("csharp web-client")]
	[Verb<CodeGenCommandAction>("typescript web-client")]
	public record class CodeGenOptions {
		[Option(Description = "Output directory for generated C# files")]
		public required DirectoryInfo OutputDirectory { get; init; }

		[Option(Description = "Project file path")]
		public required FileInfo Project { get; init; }
	}

	public class CodeGenCommandAction : CommandAction<CodeGenOptions> {
		private readonly ICodeGenerator codeGenerator;
		public CodeGenCommandAction(ICodeGenerator codeGenerator, IOptions<CodeGenOptions> options) : base(options) {
			this.codeGenerator = codeGenerator;
		}
		public override Task<int> Invoke(CancellationToken cancellationToken) {
			this.Writer.WriteLine(this.codeGenerator.Generate(this.options));
			return Task.FromResult(0);
		}
	}
}
