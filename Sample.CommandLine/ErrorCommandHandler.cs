using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb("error", typeof(ErrorCommandHandler), Description = "This command will throw an exception")]
	public record class ErrorCommandOptions {
	}
	public class ErrorCommandHandler : BaseHandler<ErrorCommandOptions> {
		public ErrorCommandHandler(IOptions<ErrorCommandOptions> options) : base(options) {
		}
		public override Task<int> Invoke(CancellationToken token) {
			throw new InvalidOperationException("We have a problem!!");
		}
	}
}
