using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	public class UseArgumentAttribute<TArgument, THandler> : ArgumentAttribute where TArgument : Argument where THandler : IAsyncOptionHandler<TArgument> {
	}
	public class UseArgumentAttribute<TArgument> : ArgumentAttribute where TArgument : Argument {
	}
}