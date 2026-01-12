using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace Albatross.CommandLine {
	/// <summary>
	/// This interface is a command context with scoped lifetime.  The same instance of the context can be injected to PreAction handlers
	/// as well as the Command handler.  It can be used to share state between those handlers.  
	/// </summary>
	public interface ICommandContext {
		/// <summary>
		/// Gets the parse result from command line parsing.
		/// </summary>
		ParseResult Result { get; }
		/// <summary>
		/// Gets the space-separated key identifying the current command in the hierarchy.
		/// </summary>
		string Key { get; }
		/// <summary>
		/// Gets a required value from the context. Throws if the value is not found or has the wrong type.
		/// </summary>
		/// <typeparam name="T">The expected type of the value.</typeparam>
		/// <param name="key">The key identifying the value.</param>
		/// <returns>The stored value.</returns>
		T GetRequiredValue<T>(string key);
		/// <summary>
		/// Gets a value from the context, or default if not found.
		/// </summary>
		/// <typeparam name="T">The expected type of the value.</typeparam>
		/// <param name="key">The key identifying the value.</param>
		/// <returns>The stored value, or default if not found.</returns>
		T? GetValue<T>(string key);
		/// <summary>
		/// Stores a value in the context for sharing between handlers.
		/// </summary>
		/// <typeparam name="T">The type of the value.</typeparam>
		/// <param name="key">The key to identify the value.</param>
		/// <param name="value">The value to store.</param>
		void SetValue<T>(string key, T value) where T : notnull;
		/// <summary>
		/// Records the status of an option handler execution.
		/// </summary>
		/// <param name="status">The status to record.</param>
		void SetInputActionStatus(OptionHandlerStatus status);
		/// <summary>
		/// Gets a value indicating whether the parse result contains errors.
		/// </summary>
		bool HasParsingError { get; }
		/// <summary>
		/// Gets a value indicating whether short-circuit options (like --help or --version) were specified.
		/// </summary>
		bool HasShortCircuitOptions { get; }
		/// <summary>
		/// Gets a value indicating whether any option handler reported an error.
		/// </summary>
		bool HasInputActionError { get; }
	}

	/// <summary>
	/// Default implementation of <see cref="ICommandContext"/> that provides state sharing between option handlers and command handlers.
	/// </summary>
	public class CommandContext : ICommandContext {
		/// <inheritdoc/>
		public string Key { get; }
		/// <inheritdoc/>
		public ParseResult Result { get; }
		private readonly Dictionary<string, object> values = new();
		private readonly Dictionary<string, OptionHandlerStatus> inputStatus = new();

		/// <inheritdoc/>
		public bool HasParsingError => Result.Errors.Count > 0;
		/// <inheritdoc/>
		public bool HasInputActionError => inputStatus.Any(x => !x.Value.Success);
		/// <inheritdoc/>
		public bool HasShortCircuitOptions => Result.CommandResult.Children.OfType<OptionResult>().Any(x => x.Option.Action?.Terminating == true);

		/// <summary>
		/// Initializes a new command context from the parse result.
		/// </summary>
		public CommandContext(ParseResult result) {
			this.Result = result;
			this.Key = result.CommandResult.Command.GetCommandKey();
		}

		public T GetRequiredValue<T>(string key) {
			if (values.TryGetValue(key, out var value)) {
				if (value is T t) {
					return t;
				} else {
					throw new InvalidOperationException($"Stored value for key {key} is not of type {typeof(T).FullName}");
				}
			} else {
				throw new KeyNotFoundException($"No value stored for key {key}");
			}
		}

		public T? GetValue<T>(string key) {
			if (values.TryGetValue(key, out var value)) {
				if (value is T t) {
					return t;
				} else {
					throw new InvalidOperationException($"Stored value for key {key} is not of type {typeof(T).FullName}");
				}
			} else {
				return default;
			}
		}

		public void SetValue<T>(string key, T value) where T : notnull {
			if (value == null) {
				throw new ArgumentException("Value cannot be null");
			}
			values[key] = value;
		}

		public void SetInputActionStatus(OptionHandlerStatus status) {
			this.inputStatus[status.Name] = status;
		}
	}
}