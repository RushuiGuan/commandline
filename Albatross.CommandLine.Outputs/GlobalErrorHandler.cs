using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Albatross.CommandLine.Outputs {
	public class GlobalErrorHandler : ICommandErrorHandler {
		private readonly ICommandContext context;

		public GlobalErrorHandler(ICommandContext context) {
			this.context = context;
		}

		// A real error outranks cancellation; cancellation-only gets the conventional
		// "terminated by Ctrl+C" code (128 + SIGINT), which also matches what
		// System.CommandLine's forced-termination path returns.
		const int ErrorExitCode = 1;
		const int CancelledExitCode = 130;

		public int? Handle(params IEnumerable<Error> errors) {
			var exitCode = 0;
			var list = new List<ErrorOutput>();
			foreach (var error in errors) {
				list.Add(new ErrorOutput {
					Source = error.Source,
					Symbol = error.Symbol,
					Message = error.Message,
					Detail = error.Exception?.Message,
				});
				if (error.Source is ErrorSource.CommandTaskCancellation or ErrorSource.OptionTaskCancellation) {
					// don't downgrade an error already recorded in this set
					if (exitCode == 0) {
						exitCode = CancelledExitCode;
					}
				} else {
					exitCode = ErrorExitCode;
				}
			}
			var output = new CommandOutput<JToken> {
				ExitCode = exitCode,
				Command = context.Key,
				Errors = list,
			};
			// Neither an error nor a cancellation is normal command output, so always to stderr.
			output.Print(null, context.Result.IsCompact(), stderr: true);
			return output.ExitCode;
		}
	}
}