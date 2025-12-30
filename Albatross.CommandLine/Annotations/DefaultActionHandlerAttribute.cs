using System;

namespace Albatross.CommandLine.Annotations {
	public class DefaultActionHandlerAttribute : Attribute {
		public DefaultActionHandlerAttribute(Type handlerType) {
			this.HandlerType = handlerType;
		}
		public Type HandlerType { get; }
	}
}