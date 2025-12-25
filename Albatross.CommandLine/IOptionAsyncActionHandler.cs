using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public interface IOptionAsyncActionHandler {
		Task<int> InvokeAsync(ParseResult result, CancellationToken cancellationToken);
	}
	
	public interface IOptionActionHandler {
		Task<int> Invoke(ParseResult result);
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class OptionActionAttribute<T> : Attribute where T:IOptionActionHandler{
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class OptionAsyncActionAttribute<T> : Attribute where T : IOptionAsyncActionHandler {
	}
	public class OptionFactory {
		Dictionary<Type, Option>  options = new Dictionary<Type, Option>();
		
		public T Create<T>()where T:Option, new() {
			return new T();
		}
	}
}