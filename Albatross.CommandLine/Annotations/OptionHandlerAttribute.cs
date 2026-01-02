using System;
using System.CommandLine;

namespace Albatross.CommandLine.Annotations {
	public class OptionHandlerAttribute : Attribute {
		public OptionHandlerAttribute(Type handlerType) {
			this.HandlerType = handlerType;
		}
		public Type HandlerType { get; }
	}

	public class OptionHandlerAttribute<THandler, TOption> : Attribute where THandler : IAsyncOptionHandler<TOption> where TOption : Option {
	}

	public class OptionHandlerAttribute<THandler, TOption, TContextValue> : Attribute 
		where THandler : IAsyncOptionHandler<TOption, TContextValue> 
		where TOption : Option, IUseContextValue<TContextValue> {
	}
}