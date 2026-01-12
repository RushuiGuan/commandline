using System;
using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	/// <summary>
	/// Marks a property to use a predefined argument type with built-in validation and parsing logic.
	/// The generated code will instantiate and configure the specified argument type.
	/// </summary>
	/// <typeparam name="TArgument">The predefined argument type to use.</typeparam>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class UseArgumentAttribute<TArgument> : ArgumentAttribute where TArgument : Argument {
	}
}