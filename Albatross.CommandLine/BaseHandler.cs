using System;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public abstract class BaseHandler<T> : IAsyncCommandHandler where T : class {
		protected ParseResult result;
		protected readonly T parameters;
		protected virtual TextWriter Writer => result.InvocationConfiguration.Output;

		protected BaseHandler(ParseResult result, T parameters){
			this.result = result;
			this.parameters = parameters;	
		}

		public abstract Task<int> InvokeAsync(CancellationToken cancellationToken);
	}
}