using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<ExampleBaseSpecificRegistrationsHandler>("example csharp web-client")]
	[Verb<ExampleBaseSpecificRegistrationsHandler>("example typescript web-client")]
	public record class ExampleCommandSpecificRegistrationsParams {
		[Option(Description = "Output directory for generated C# files")]
		public required DirectoryInfo OutputDirectory { get; init; }

		[Option(Description = "Project file path")]
		public required FileInfo Project { get; init; }
	}

	public class ExampleBaseSpecificRegistrationsHandler : BaseHandler<ExampleCommandSpecificRegistrationsParams> {
		private readonly ICodeGenerator codeGenerator;
		public ExampleBaseSpecificRegistrationsHandler(ICodeGenerator codeGenerator, ParseResult result, ExampleCommandSpecificRegistrationsParams parameters) : base(result, parameters) {
			this.codeGenerator = codeGenerator;
		}
		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			this.Writer.WriteLine(this.codeGenerator.Generate(this.parameters));
			return Task.FromResult(0);
		}
	}
}
