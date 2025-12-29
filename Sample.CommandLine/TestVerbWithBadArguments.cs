using Albatross.CommandLine;
using System.Threading;
using System.Threading.Tasks;

[assembly:Verb<int[], DefaultAsyncCommandHandler<string>>("test assembly-verb-with-bad-arguments", Description = "The second generic argument is not a valid parameters class")]
namespace Sample.CommandLine {
	public abstract class AbstractAsyncCommandHandler : IAsyncCommandHandler {
		public abstract Task<int> InvokeAsync(CancellationToken cancellationToken);
	}

	// [Verb<AbstractCommandHandler>("test class-verb-with-bad-handler", Description = "The command action class is abstract")]
	public class TestVerbWithBadArgumentsParams {
		// generator should report errors for the above two cases
		// this could also cause a runtime error
	}
}