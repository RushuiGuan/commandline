using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	public class UseArgumentAttribute<TArgument> : ArgumentAttribute where TArgument : Argument {
	}
}