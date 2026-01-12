using System;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	/// <summary>
	/// Handler for processing command options or arguments as pre-actions before the main command handler executes.
	/// Use this interface for handlers that perform side effects without returning a value.
	/// </summary>
	/// <typeparam name="T">The type of the option being handled.</typeparam>
	public interface IAsyncOptionHandler<in T> where T : Option {
		/// <summary>
		/// Processes the option asynchronously.
		/// </summary>
		/// <param name="symbol">The option being processed.</param>
		/// <param name="result">The parse result containing the option's value.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		Task InvokeAsync(T symbol, ParseResult result, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Handler for processing command options that returns a value to be stored in the command context.
	/// The returned value is automatically stored and can be retrieved by subsequent handlers or the command handler.
	/// </summary>
	/// <typeparam name="TOption">The type of the option being handled.</typeparam>
	/// <typeparam name="TContextValue">The type of the value to store in the context.</typeparam>
	public interface IAsyncOptionHandler<in TOption, TContextValue> where TOption : Option where TContextValue : notnull {
		/// <summary>
		/// Processes the option asynchronously and returns a result to store in the context.
		/// </summary>
		/// <param name="symbol">The option being processed.</param>
		/// <param name="result">The parse result containing the option's value.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A result containing the value to store, or an empty result if no value should be stored.</returns>
		Task<OptionHandlerResult<TContextValue>> InvokeAsync(TOption symbol, ParseResult result, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Wraps the result of an option handler that may or may not produce a value.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	public record OptionHandlerResult<T> where T : notnull {
		/// <summary>
		/// Gets a value indicating whether this result contains a value.
		/// </summary>
		public bool HasValue { get; }
		T? value;

		/// <summary>
		/// Creates an empty result with no value.
		/// </summary>
		public OptionHandlerResult() {
			HasValue = false;
			value = default;
		}

		/// <summary>
		/// Creates a result containing the specified value.
		/// </summary>
		/// <param name="value">The value to wrap.</param>
		public OptionHandlerResult(T value) {
			HasValue = true;
			this.value = value;
		}

		/// <summary>
		/// Gets the contained value. Throws if <see cref="HasValue"/> is false.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when accessing the value when <see cref="HasValue"/> is false.</exception>
		public T Value => HasValue ? value! : throw new InvalidOperationException("Value has not been set");
	}
}