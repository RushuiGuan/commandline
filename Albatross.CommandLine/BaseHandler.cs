using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class BaseHandler<T> : ICommandHandler where T : class {
		protected readonly T options;
		protected virtual TextWriter writer => Console.Out;

		public BaseHandler(IOptions<T> options) {
			this.options = options.Value;
		}
		public virtual int Invoke(ParseResult result) {
			this.writer.WriteLine(result.ToString());
			return 0;
		}
	}

	public class BaseAsyncHandler<T> : IAsyncCommandHandler where T : class {
		protected readonly T options;
		protected virtual TextWriter writer => Console.Out;

		public BaseAsyncHandler(IOptions<T> options) {
			this.options = options.Value;
		}
		
		public Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken) {
			this.writer.WriteLine(result.ToString());
			return Task.FromResult(0);
		}
	}
}