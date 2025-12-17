using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class DefaultCommandAction : ICommandAction {
		private readonly ParseResult result;

		public DefaultCommandAction(ParseResult result) {
			this.result = result;
		}

		public Task<int> Invoke(CancellationToken cancellationToken) {
			result.InvocationConfiguration.Output.Write(result.ToString());
			return Task.FromResult(0);
		}
	}
}