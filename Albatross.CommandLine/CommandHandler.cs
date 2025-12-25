using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public abstract class CommandHandler<T> : ICommandHandler where T : class {
		private readonly ParseResult? result;
		protected ParseResult Result => result ?? throw new InvalidOperationException("ParseResult is not available.");
		protected readonly T options;
		protected virtual TextWriter Writer => Console.Out;

		protected CommandHandler(ParseResult result, T options) : this(options){
			this.result = result;
		}

		protected CommandHandler(T options) {
			this.options = options;
		}

		public abstract Task<int> InvokeAsync(CancellationToken cancellationToken);
	}
}