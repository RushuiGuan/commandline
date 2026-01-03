using System;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	/// <summary>
	/// This handler is for command option or argument action.  If the handler has a return value, inject ICommandContext to set the value into the context.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IAsyncOptionHandler<in T> where T : Option {
		Task InvokeAsync(T symbol, ParseResult result, CancellationToken cancellationToken);
	}

	public interface IAsyncOptionHandler<in TOption, TContextValue> where TOption : Option where TContextValue : notnull {
		Task<OptionHandlerResult<TContextValue>> InvokeAsync(TOption symbol, ParseResult result, CancellationToken cancellationToken);
	}
	public record OptionHandlerResult<T> where T : notnull {
		public bool HasValue { get; }
		T? value;

		public OptionHandlerResult() {
			HasValue = false;
			value = default;
		}
		public OptionHandlerResult(T value) {
			HasValue = true;
			this.value = value;
		}
		public T Value => HasValue ? value! : throw new InvalidOperationException("Value has not been set");
	}
}