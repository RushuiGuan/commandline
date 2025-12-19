using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<ExampleCommandSpecificRegistrationsAction>("example csharp web-client")]
	[Verb<ExampleCommandSpecificRegistrationsAction>("example typescript web-client")]
	public record class ExampleCommandSpecificRegistrationsOptions {
		[Option(Description = "Output directory for generated C# files")]
		public required DirectoryInfo OutputDirectory { get; init; }

		[Option(Description = "Project file path")]
		public required FileInfo Project { get; init; }
	}

	public class ExampleCommandSpecificRegistrationsAction : CommandAction<ExampleCommandSpecificRegistrationsOptions> {
		private readonly ICodeGenerator codeGenerator;
		public ExampleCommandSpecificRegistrationsAction(ICodeGenerator codeGenerator, IOptions<ExampleCommandSpecificRegistrationsOptions> options) : base(options) {
			this.codeGenerator = codeGenerator;
		}
		public override Task<int> Invoke(CancellationToken cancellationToken) {
			this.Writer.WriteLine(this.codeGenerator.Generate(this.options));
			return Task.FromResult(0);
		}
	}
}
