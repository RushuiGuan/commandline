using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class DefaultCommandHandler : ICommandHandler {
		public int Invoke(InvocationContext context) {
			context.Console.Out.WriteLine(context.ParseResult.ToString());
			return 0;
		}

		public Task<int> InvokeAsync(InvocationContext context)
			=> Task.FromResult(Invoke(context));
	}
}
