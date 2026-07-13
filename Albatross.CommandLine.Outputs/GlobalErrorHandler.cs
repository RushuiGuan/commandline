using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Albatross.CommandLine.Outputs {
	public class GlobalErrorHandler : ICommandErrorHandler {
		private readonly ICommandContext context;

		public GlobalErrorHandler(ICommandContext context) {
			this.context = context;
		}

		public int? Handle(params IEnumerable<Error> errors) {
			var output = new CommandOutput<JToken> {
				ExitCode = 1,
				Command = context.Key,
				Errors = errors.Select(x => new ErrorOutput {
					Source = x.Source,
					Key = x.Key,
					Message = x.Message,
					Detail = x.Exception?.Message,
				}).ToArray(),
			};
			output.Print(null, context.Result.IsCompact(), stderr: true);
			return output.ExitCode;
		}
	}
}