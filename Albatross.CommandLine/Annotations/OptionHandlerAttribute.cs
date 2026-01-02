using System;

namespace Albatross.CommandLine.Annotations {
	public class OptionHandlerAttribute : Attribute {
		public OptionHandlerAttribute(Type handlerType) {
			this.HandlerType = handlerType;
		}
		public Type HandlerType { get; }
	}
}