using Albatross.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<ExampleCommandSpecificRegistrationsHandler>("example csharp web-client")]
	[Verb<ExampleCommandSpecificRegistrationsHandler>("example typescript web-client")]
	public record class ExampleCommandSpecificRegistrationsOptions {
		[Option(Description = "Output directory for generated C# files")]
		public required DirectoryInfo OutputDirectory { get; init; }

		[Option(Description = "Project file path")]
		public required FileInfo Project { get; init; }
	}

	public class ExampleCommandSpecificRegistrationsHandler : CommandHandler<ExampleCommandSpecificRegistrationsOptions> {
		private readonly ICodeGenerator codeGenerator;
		public ExampleCommandSpecificRegistrationsHandler(ICodeGenerator codeGenerator, ExampleCommandSpecificRegistrationsOptions options) : base(options) {
			this.codeGenerator = codeGenerator;
		}
		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			this.Writer.WriteLine(this.codeGenerator.Generate(this.options));
			return Task.FromResult(0);
		}
	}
}
