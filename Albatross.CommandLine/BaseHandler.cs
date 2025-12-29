using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public abstract class BaseHandler<T> : IAsyncCommandHandler where T : class {
		protected ParseResult result;
		protected readonly T options;
		protected virtual TextWriter Writer => Console.Out;

		protected BaseHandler(ParseResult result, T options){
			this.result = result;
			this.options = options;	
		}

		public abstract Task<int> InvokeAsync(CancellationToken cancellationToken);
	}
}