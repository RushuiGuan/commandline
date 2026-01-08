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
		ParseResult Result { get; }
		string Key { get; }
		T GetRequiredValue<T>(string key);
		T? GetValue<T>(string key);
		void SetValue<T>(string key, T value) where T : notnull;
		void SetInputActionStatus(OptionHandlerStatus status);
		bool HasParsingError { get; }
		bool HasShortCircuitOptions { get; }
		bool HasInputActionError { get; }
	}

	public class CommandContext : ICommandContext {
		public string Key { get; }
		public ParseResult Result { get; }
		private readonly Dictionary<string, object> values = new();
		private readonly Dictionary<string, OptionHandlerStatus> inputStatus = new();

		public bool HasParsingError => Result.Errors.Count > 0;
		public bool HasInputActionError => inputStatus.Any(x => !x.Value.Success);
		public bool HasShortCircuitOptions => Result.CommandResult.Children.OfType<OptionResult>().Any(x => x.Option.Action?.Terminating == true);

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