using System;
using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	//TODO: TOption has to be the same class as the attribute target.  need a warning if not
	[AttributeUsage(AttributeTargets.Class, AllowMultiple =false)]
	public class OptionHandlerAttribute<TOption, THandler> : Attribute
		where TOption : Option
		where THandler : IAsyncOptionHandler<TOption> {
	}

	//TODO: TOption has to be the same class as the attribute target.  need a warning if not
	[AttributeUsage(AttributeTargets.Class, AllowMultiple =false)]
	public class OptionHandlerAttribute<TOption, THandler, TContextValue> : Attribute
		where TOption : Option
		where THandler : IAsyncOptionHandler<TOption, TContextValue>
		where TContextValue : notnull {
	}
}