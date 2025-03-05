using Albatross.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb("di-error", typeof(TestDIExceptionCommandHandler),
		Description = "The handler of this command cannot be constructed by dependency injection because of its invalid dependency")]
	public class TestDIExceptionCommandOptions { }

	public class TestDIExceptionCommandHandler : ICommandHandler {
		public TestDIExceptionCommandHandler(string data) { }

		public int Invoke(InvocationContext context) => 0;
		public Task<int> InvokeAsync(InvocationContext context) => Task.FromResult(0);
	}
}