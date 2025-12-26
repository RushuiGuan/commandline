using Albatross.CommandLine;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<ExampleBaseSpecificRegistrationsHandler>("example csharp web-client")]
	[Verb<ExampleBaseSpecificRegistrationsHandler>("example typescript web-client")]
	public record class ExampleCommandSpecificRegistrationsOptions {
		[Option(Description = "Output directory for generated C# files")]
		public required DirectoryInfo OutputDirectory { get; init; }

		[Option(Description = "Project file path")]
		public required FileInfo Project { get; init; }
	}

	public class ExampleBaseSpecificRegistrationsHandler : BaseHandler<ExampleCommandSpecificRegistrationsOptions> {
		private readonly ICodeGenerator codeGenerator;
		public ExampleBaseSpecificRegistrationsHandler(ICodeGenerator codeGenerator, ParseResult result, ExampleCommandSpecificRegistrationsOptions options) : base(result, options) {
			this.codeGenerator = codeGenerator;
		}
		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			this.Writer.WriteLine(this.codeGenerator.Generate(this.options));
			return Task.FromResult(0);
		}
	}
}
