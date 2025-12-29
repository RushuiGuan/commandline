using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class DefaultAsyncCommandHandler<T> : IAsyncCommandHandler where T : class {
		private readonly T parameters;
		private readonly ParseResult result;

		public DefaultAsyncCommandHandler(T parameters, ParseResult result) {
			this.parameters = parameters;
			this.result = result;
		}

		public async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			await Console.Out.WriteLineAsync($"Command Class: {result.CommandResult.Command.GetType().FullName}");
			await Console.Out.WriteLineAsync($"Command Result: {result}");
			await Console.Out.WriteLineAsync(parameters.ToString());
			return 0;
		}
	}
}