using Albatross.CommandLine;
using Albatross.CommandLine.Annotations;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestErrorHandling>("test error-handling", Description = "This is a test command for error handling.")]
	public record TestErrorHandlingParams {
	}
	public class TestErrorHandling : BaseHandler<TestErrorHandlingParams> {
		public TestErrorHandling(ParseResult result, TestErrorHandlingParams parameters) : base(result, parameters) {
		}

		public override Task<int> InvokeAsync(CancellationToken cancellationToken) {
			throw new InvalidOperationException("this should fail"); 
		}
	}
}