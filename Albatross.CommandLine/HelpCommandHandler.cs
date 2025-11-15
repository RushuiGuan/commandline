using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public interface ICommandHandler {
		Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken);
		int Invoke(ParseResult result);
	}
	public class HelpCommandHandler : ICommandHandler {
		public int Invoke(ParseResult result) {
			result.InvocationConfiguration.Output.Write(result.CommandResult.Command);
			return 0;
		}

		public Task<int> InvokeAsync(ParseResult context) {
			Invoke(context);
			return Task.FromResult(0);
		}

		public Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken) {
			return Task.FromResult(Invoke(result));
		}
	}
}