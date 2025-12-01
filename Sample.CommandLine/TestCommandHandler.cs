using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.Threading;
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
		private readonly ParseResult result;
		private readonly TestCommandOptions options;

		public TestCommandHandler(ILogger logger, IOptions<TestCommandOptions> options, ParseResult result) {
			this.logger = logger;
			this.result = result;
			this.options = options.Value;
		}

		public Task<int> Invoke(CancellationToken token) {
			return Task.FromResult(0);
		}
	}
}