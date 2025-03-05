using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class HelpCommandHandler : ICommandHandler {
		public int Invoke(InvocationContext context) {
			using var writer = context.Console.Out.CreateTextWriter();
			context.HelpBuilder.Write(context.ParseResult.CommandResult.Command, writer);
			return 0;
		}

		public Task<int> InvokeAsync(InvocationContext context) {
			Invoke(context);
			return Task.FromResult(0);
		}
	}
}