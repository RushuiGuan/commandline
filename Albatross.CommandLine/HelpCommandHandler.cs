using Microsoft.Extensions.Primitives;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class HelpCommandHandler : ICommandHandler {
		private readonly ParseResult result;

		public HelpCommandHandler(ParseResult result) {
			this.result = result;
		}

		public Task<int> Invoke(CancellationToken cancellationToken) {
			result.InvocationConfiguration.Output.Write(result.ToString());
			return Task.FromResult(0);
		}
	}
}