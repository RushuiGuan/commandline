using Albatross.CommandLine;
using System.Threading;
using System.Threading.Tasks;

[assembly:Verb<DefaultCommandAction<string>, int[]>("test assembly-verb-with-bad-arguments", Description = "The second generic argument is not a valid options class")]
namespace Sample.CommandLine {
	public abstract class AbstractCommandAction : ICommandAction {
		public abstract Task<int> Invoke(CancellationToken cancellationToken);
	}

	// [Verb<AbstractCommandAction>("test class-verb-with-bad-handler", Description = "The command action class is abstract")]
	public class TestVerbWithBadArgumentsOptions {
		// generator should report errors for the above two cases
		// this could also cause a runtime error
	}
}