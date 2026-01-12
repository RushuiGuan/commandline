using System;
using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	/// <summary>
	/// Associates an option class with a handler that processes the option value before command execution.
	/// Apply this attribute to an Option class to specify its handler.
	/// </summary>
	/// <typeparam name="TOption">The option type (must be the same as the attributed class).</typeparam>
	/// <typeparam name="THandler">The handler type that processes this option.</typeparam>
	//TODO: TOption has to be the same class as the attribute target.  need a warning if not
	[AttributeUsage(AttributeTargets.Class, AllowMultiple =false)]
	public class OptionHandlerAttribute<TOption, THandler> : Attribute
		where TOption : Option
		where THandler : IAsyncOptionHandler<TOption> {
	}

	/// <summary>
	/// Associates an option class with a handler that processes the option value and stores a result in the command context.
	/// Apply this attribute to an Option class to specify its handler and the type of value it produces.
	/// </summary>
	/// <typeparam name="TOption">The option type (must be the same as the attributed class).</typeparam>
	/// <typeparam name="THandler">The handler type that processes this option.</typeparam>
	/// <typeparam name="TContextValue">The type of value the handler produces and stores in the context.</typeparam>
	//TODO: TOption has to be the same class as the attribute target.  need a warning if not
	[AttributeUsage(AttributeTargets.Class, AllowMultiple =false)]
	public class OptionHandlerAttribute<TOption, THandler, TContextValue> : Attribute
		where TOption : Option
		where THandler : IAsyncOptionHandler<TOption, TContextValue>
		where TContextValue : notnull {
	}
}