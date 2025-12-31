using Albatross.CommandLine;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine.MutuallyExclusiveParams {
	public class ExampleProjectBaseHandler : BaseHandler<ProjectParams> {
		public ExampleProjectBaseHandler(ParseResult result,ProjectParams parameters) : base(result, parameters) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			if (parameters is ProjectEchoOptions echoParams) {
				this.Writer.WriteLine($"Invoked project echo: {echoParams}");
			} else if (parameters is ProjectFubarOptions fubarParams) {
				this.Writer.WriteLine($"Invoked project fubar: {fubarParams}");
			} else {
				throw new NotSupportedException($"Unsupported parameters: {parameters}");
			}
			return Task.FromResult(0);
		}
	}
}