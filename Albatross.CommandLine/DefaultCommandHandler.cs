using System.CommandLine;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class DefaultCommandHandler : ICommandHandler {
		public int Invoke(ParseResult result) {
			result.InvocationConfiguration.Output.Write(result.ToString());
			return 0;
		}
	}
}