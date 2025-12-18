using Albatross.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace Sample.CommandLine {
	[Verb<TestDIExceptionCommandAction>("di-error",
		Description = "The handler of this command cannot be constructed by dependency injection because of its invalid dependency")]
	public class TestDIExceptionCommandOptions { }

	public class TestDIExceptionCommandAction : ICommandAction {
		public TestDIExceptionCommandAction(string data) { }

		public Task<int> Invoke(CancellationToken token) => Task.FromResult(0);
	}
}