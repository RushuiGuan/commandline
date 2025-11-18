using System.CommandLine;

namespace Albatross.CommandLine {
	public class HelpCommandHandler : ICommandHandler {
		public int Invoke(ParseResult result) {
			result.InvocationConfiguration.Output.Write(result.ToString());
			return 0;
		}
	}
}