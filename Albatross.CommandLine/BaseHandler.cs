using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class BaseHandler<T> : ICommandHandler where T : class {
		protected readonly T options;
		protected virtual TextWriter writer => Console.Out;

		public BaseHandler(IOptions<T> options) {
			this.options = options.Value;
		}
		public virtual int Invoke(InvocationContext context) {
			context.Console.WriteLine(context.ParseResult.ToString());
			return 0;
		}
		public virtual Task<int> InvokeAsync(InvocationContext context) {
			return Task.FromResult(Invoke(context));
		}
	}
}