using Albatross.CommandLine;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.MutuallyExclusiveOptions {
	public class ExampleProjectBaseHandler : BaseHandler<ProjectOptions> {
		public ExampleProjectBaseHandler(ParseResult result,ProjectOptions options) : base(result, options) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			if (options is ProjectEchoOptions echoOptions) {
				this.Writer.WriteLine($"Invoked project echo: {echoOptions}");
			} else if (options is ProjectFubarOptions fubarOptions) {
				this.Writer.WriteLine($"Invoked project fubar: {fubarOptions}");
			} else {
				throw new NotSupportedException($"Unsupported options: {options}");
			}
			return Task.FromResult(0);
		}
	}
}