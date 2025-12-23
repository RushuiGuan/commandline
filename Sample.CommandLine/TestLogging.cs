using Albatross.CommandLine;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestLogging>("test logging", Description = "Use this command to verify logging outputs")]
	public record class TestLoggingOptions {
	}
	public class TestLogging : CommandAction<TestLoggingOptions> {
		private readonly ILogger<TestLogging> logger;

		public TestLogging(ILogger<TestLogging> logger, TestLoggingOptions options) : base(options) {
			this.logger = logger;
		}

		public override Task<int> Invoke(CancellationToken cancellationToken) {
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