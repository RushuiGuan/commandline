using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Sample.CommandLine.Services;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.CommandSpecificRegistrations {
	/// <summary>
	/// find the command specific registration in <see cref="Program"/> class
	/// </summary>
	[Verb<CodeGenHandler>("csharp web-client")]
	[Verb<CodeGenHandler>("typescript web-client")]
	public record class CodeGenParams {
		[Argument]
		public required FileInfo Project { get; init; }

		[Option]
		public required DirectoryInfo OutputDirectory { get; init; }
	}

	public class CodeGenHandler : BaseHandler<CodeGenParams> {
		private readonly ICodeGenerator codeGenerator;
		public CodeGenHandler(ICodeGenerator codeGenerator, ParseResult result, CodeGenParams parameters) : base(result, parameters) {
			this.codeGenerator = codeGenerator;
		}
		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			this.Writer.WriteLine(this.codeGenerator.Generate(this.parameters));
			return Task.FromResult(0);
		}
	}
}
