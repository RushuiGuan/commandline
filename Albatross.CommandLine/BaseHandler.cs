using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public abstract class BaseHandler<T> : ICommandHandler where T : class {
		protected readonly T options;
		readonly ParseResult? result;
		protected readonly string? format;
		protected virtual TextWriter writer => Console.Out;
		protected ParseResult Result => result ?? throw new InvalidOperationException("ParseResult is not available.");

		protected BaseHandler(ParseResult result, IOptions<T> options) : this(options){
			this.result = result;
			format = result.GetValue<string?>("--format");
		}

		protected BaseHandler(IOptions<T> options) {
			this.options = options.Value;
		}

		public abstract Task<int> Invoke(CancellationToken cancellationToken);
	}
}