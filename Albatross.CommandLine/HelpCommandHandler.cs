using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class HelpCommandAction :ICommandAction {
		private readonly ParseResult result;

		public HelpCommandAction(ParseResult result) {
			this.result = result;
		}

		public Task<int> Invoke(CancellationToken _) {
			return Task.FromResult(0);
		}
	}
}