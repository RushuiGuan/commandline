using System.CommandLine;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class DefaultCommandHandler : ICommandHandler {
		public int Invoke(ParseResult result) {
			result.Console.Out.WriteLine(result.ParseResult.ToString());
			return 0;
		}

		public Task<int> InvokeAsync(InvocationContext context)
			=> Task.FromResult(Invoke(context));
	}
}
