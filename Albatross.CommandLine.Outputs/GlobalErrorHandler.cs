using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Albatross.CommandLine.Outputs {
	public class GlobalErrorHandler : ICommandErrorHandler {
		private readonly ICommandContext context;
		private readonly ILogger<GlobalErrorHandler> logger;

		public GlobalErrorHandler(ICommandContext context, ILogger<GlobalErrorHandler> logger) {
			this.context = context;
			this.logger = logger;
		}
		public int? Handle(Exception exception) {
			logger.LogError(exception, $"error executing command {context.Key}");
			var output = new CommandOutput<JToken> {
				ExitCode = 1,
				Command = context.Key,
				Error = exception.GetType().Name,
				ErrorDetail = exception.Message,
			};

			output.Print(null, context.Result.IsCompact(), stderr: true);
			return output.ExitCode;
		}
	}
}
