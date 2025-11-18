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
		
		public Task<int> InvokeAsync(CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}
	}
}