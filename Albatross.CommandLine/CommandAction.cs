using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public abstract class CommandAction<T> : ICommandAction where T : class {
		private readonly ParseResult? result;
		protected ParseResult Result => result ?? throw new InvalidOperationException("ParseResult is not available.");
		protected readonly T options;
		protected readonly string? format;
		protected virtual TextWriter Writer => Console.Out;

		protected CommandAction(ParseResult result, T options) : this(options){
			this.result = result;
			format = result.GetValue<string?>("--format");
		}

		protected CommandAction(T options) {
			this.options = options;
		}

		public abstract Task<int> Invoke(CancellationToken cancellationToken);
	}
}