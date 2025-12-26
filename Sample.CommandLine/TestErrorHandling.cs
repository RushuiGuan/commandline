using Albatross.CommandLine;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestErrorHandling>("test error-handling", Description = "This is a test command for error handling.")]
	public record TestErrorHandlingOptions {
	}
	public class TestErrorHandling : BaseHandler<TestErrorHandlingOptions> {
		public TestErrorHandling(ParseResult result, TestErrorHandlingOptions options) : base(result, options) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			throw new InvalidOperationException("this should fail"); 
		}
	}
}