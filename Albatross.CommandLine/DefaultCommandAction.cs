using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class DefaultCommandAction<T> : ICommandAction where T : class {
		private readonly T options;
		private readonly ParseResult result;

		public DefaultCommandAction(IOptions<T> options, ParseResult result) {
			this.options = options.Value;
			this.result = result;
		}

		public Task<int> Invoke(CancellationToken cancellationToken) {
			result.InvocationConfiguration.Output.WriteLine(result.ToString());
			Console.Out.WriteLine(options.ToString());
			return Task.FromResult(0);
		}
	}
}