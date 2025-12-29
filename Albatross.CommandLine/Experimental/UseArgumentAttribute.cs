using System.CommandLine;

namespace Albatross.CommandLine {
	public class UseArgumentAttribute<TArgument, THandler> : ArgumentAttribute where TArgument : Argument where THandler : IAsyncCommandParameterHandler<TArgument> {
	}
	public class UseArgumentAttribute<TArgument> : ArgumentAttribute where TArgument : Argument {
	}
}