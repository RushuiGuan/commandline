using Albatross.CommandLine;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb("error", typeof(ErrorCommandHandler), Description = "This command will throw an exception")]
	public record class ErrorCommandOptions {
	}
	public class ErrorCommandHandler : BaseHandler<ErrorCommandOptions> {
		public ErrorCommandHandler(IOptions<ErrorCommandOptions> options) : base(options) {
		}
		public override int Invoke(InvocationContext context) {
			throw new InvalidOperationException("We have a problem!!");
		}
	}
}
