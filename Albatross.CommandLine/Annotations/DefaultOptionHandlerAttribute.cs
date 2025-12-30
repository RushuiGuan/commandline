using System;

namespace Albatross.CommandLine.Annotations {
	public class DefaultOptionHandlerAttribute : Attribute {
		public DefaultOptionHandlerAttribute(Type handlerType) {
			this.HandlerType = handlerType;
		}
		public Type HandlerType { get; }
	}
}