using System;
using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class UseArgumentAttribute<TArgument> : ArgumentAttribute where TArgument : Argument {
	}
}