using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	/// <summary>
	/// A default command handler that outputs diagnostic information about the command invocation.
	/// Useful for testing and debugging command configurations.
	/// </summary>
	/// <typeparam name="T">The type of the command parameters class.</typeparam>
	public class DefaultAsyncCommandHandler<T> : IAsyncCommandHandler where T : class {
		private readonly T parameters;
		private readonly ParseResult result;

		/// <summary>
		/// Initializes a new instance of the default handler with the parameters and parse result.
		/// </summary>
		public DefaultAsyncCommandHandler(T parameters, ParseResult result) {
			this.parameters = parameters;
			this.result = result;
		}

		/// <summary>
		/// Outputs the command class name, parse result, and parameter values to the console.
		/// </summary>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>Always returns 0 (success).</returns>
		public async Task<int> InvokeAsync(CancellationToken cancellationToken) {
			await Console.Out.WriteLineAsync($"Command Class: {result.CommandResult.Command.GetType().FullName}");
			await Console.Out.WriteLineAsync($"Command Result: {result}");
			await Console.Out.WriteLineAsync(parameters.ToString());
			return 0;
		}
	}
}