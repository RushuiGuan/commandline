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
			var returnCode = Invoke(this.result);
			return Task.FromResult(returnCode);
		}

		public static int Invoke(ParseResult result) {
			var helpAction = new System.CommandLine.Help.HelpAction();
			var returnCode = helpAction.Invoke(result);
			return returnCode;
		}
	}
}