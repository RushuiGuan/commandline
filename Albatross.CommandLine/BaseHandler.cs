using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	/// <summary>
	/// Abstract base class for command handlers that provides common functionality for processing parsed command parameters.
	/// Inherit from this class to implement custom command handlers with strongly-typed parameter access.
	/// </summary>
	/// <typeparam name="T">The type of the command parameters class.</typeparam>
	public abstract class BaseHandler<T> : IAsyncCommandHandler where T : class {
		/// <summary>
		/// The parse result from command line parsing.
		/// </summary>
		protected ParseResult result;
		/// <summary>
		/// The strongly-typed command parameters.
		/// </summary>
		protected readonly T parameters;
		/// <summary>
		/// Gets the text writer for command output. Defaults to the invocation configuration output.
		/// </summary>
		protected virtual TextWriter Writer => result.InvocationConfiguration.Output;

		/// <summary>
		/// Initializes a new instance of the handler with the parse result and parameters.
		/// </summary>
		protected BaseHandler(ParseResult result, T parameters){
			this.result = result;
			this.parameters = parameters;
		}

		/// <inheritdoc/>
		public abstract Task<int> InvokeAsync(CancellationToken cancellationToken);
	}
}