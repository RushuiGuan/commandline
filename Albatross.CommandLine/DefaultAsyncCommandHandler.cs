using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class DefaultAsyncCommandHandler<T> : IAsyncCommandHandler where T : class {
		private readonly T options;
		private readonly ParseResult result;

		public DefaultAsyncCommandHandler(T options, ParseResult result) {
			this.options = options;
			this.result = result;
		}

		public async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			await Console.Out.WriteLineAsync($"Command Class: {result.CommandResult.Command.GetType().FullName}");
			await Console.Out.WriteLineAsync($"Command Result: {result}");
			await Console.Out.WriteLineAsync(options.ToString());
			return 0;
		}
	}
}