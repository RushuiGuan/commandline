using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class DefaultCommandAction<T> : ICommandAction where T : class {
		private readonly T options;
		private readonly ParseResult result;

		public DefaultCommandAction(T options, ParseResult result) {
			this.options = options;
			this.result = result;
		}

		public async Task<int> Invoke(CancellationToken cancellationToken) {
			await Console.Out.WriteLineAsync($"Command Class: {result.CommandResult.Command.GetType().FullName}");
			await Console.Out.WriteLineAsync($"Command Result: {result}");
			await Console.Out.WriteLineAsync(options.ToString());
			
			return 0;
		}
	}
}