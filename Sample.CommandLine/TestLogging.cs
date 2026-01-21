using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestLogging>("test logging", Description = "Use this command to verify logging outputs")]
	public record class TestLoggingParams {
	}

	[Verb<TestLogging>("test default-logging", Description = "This command has a default logging of debug")]
	public record class TestDefaultLoggingParams {
	}

	public partial class TestDefaultLoggingCommand {
		/// <summary>
		/// add a verbosity option with default value of Debug to override the global recursive default of Error
		/// </summary>
		partial void Initialize() {
			var myOwnVerbosityOption = new VerbosityOption {
				DefaultValueFactory = _ => VerbosityOption.Debug
			};
			this.Add(myOwnVerbosityOption);
		}
	}

	public class TestLogging : BaseHandler<TestLoggingParams> {
		private readonly ILogger<TestLogging> logger;

		public TestLogging(ILogger<TestLogging> logger, ParseResult result, TestLoggingParams parameters) : base(result, parameters) {
			this.logger = logger;
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			logger.LogTrace("This is a Trace log");
			logger.LogDebug("This is a Debug log");
			logger.LogInformation("This is an Information log");
			logger.LogWarning("This is a Warning log");
			logger.LogError("This is an Error log");
			logger.LogCritical("This is a Critical log");
			return Task.FromResult(0);
		}
	}
}