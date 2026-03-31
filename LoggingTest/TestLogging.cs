using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace LoggingTest {
	[Verb<TestLoggingHandler>("test-logging", Description = "Test logging at different verbosity levels")]
	public record class TestLoggingParams {
		[Option(DefaultToInitializer = true, Description = "Message to log")]
		public string Message { get; init; } = "Test message";

		[Option(DefaultToInitializer = true, Description = "Number of times to repeat the log")]
		public int Count { get; init; } = 1;
	}

	public class TestLoggingHandler : BaseHandler<TestLoggingParams> {
		private readonly MyService service;
		private readonly ILogger<TestLoggingHandler> _logger;

		public TestLoggingHandler(MyService service, ParseResult result, TestLoggingParams parameters, ILogger<TestLoggingHandler> logger)
			: base(result, parameters) {
			this.service = service;
			_logger = logger;
		}

		public override async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			for (int i = 0; i < parameters.Count; i++) {
				_logger.LogTrace("TRACE: {Message} ({Index})", parameters.Message, i + 1);
				_logger.LogDebug("DEBUG: {Message} ({Index})", parameters.Message, i + 1);
				_logger.LogInformation("INFO: {Message} ({Index})", parameters.Message, i + 1);
				_logger.LogWarning("WARNING: {Message} ({Index})", parameters.Message, i + 1);
				_logger.LogError("ERROR: {Message} ({Index})", parameters.Message, i + 1);
				_logger.LogCritical("CRITICAL: {Message} ({Index})", parameters.Message, i + 1);
			}

			await Writer.WriteLineAsync($"Logged {parameters.Count} message(s) at all verbosity levels");
			return 0;
		}
	}
}
