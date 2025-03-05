using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb("test", typeof(TestCommandHandler), Description = "A Test Command")]
	public record class TestCommandOptions {
		// Name is a required option by default since the property is not nullable
		public string Name { get; set; } = string.Empty;

		// Description is optional since the property is nullable
		public string? Description { get; set; }

		// The OptionAttribute can be used the change the default requirement behavior.  In this case, changing the Id option to be optional
		[Option(Required = false)]
		public int Id { get; set; }
	}
	public class TestCommandHandler : ICommandHandler {
		private readonly ILogger logger;
		private readonly TestCommandOptions options;

		public TestCommandHandler(ILogger logger, IOptions<TestCommandOptions> options) {
			this.logger = logger;
			this.options = options.Value;
		}

		public int Invoke(InvocationContext context) {
			context.Console.WriteLine(context.ParseResult.Diagram());
			return 0;
		}

		public Task<int> InvokeAsync(InvocationContext context) {
			return Task.FromResult(Invoke(context));
		}
	}
}