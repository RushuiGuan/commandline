using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Albatross.CommandLine {
	/// <summary>
	/// This interface is used to create a context bewteen the Option\Argument actions to Command action
	/// </summary>
	public interface ICommandContext {
		ParseResult Result { get; }
		string Key { get; }
		T GetRequiredValue<T>(string key);
		T? GetValue<T>(string key);
		void SetValue<T>(string key, T value);
		void SetInputActionStatus(InputActionStatus status);
		bool HasParsingError { get; }
		bool HasInputActionError { get; }
	}

	public class CommandContext : ICommandContext {
		public string Key { get; }
		public ParseResult Result { get; }
		private readonly Dictionary<string, object> values = new();
		private readonly Dictionary<string, InputActionStatus> inputStatus = new();

		public bool HasParsingError => Result.Errors.Count > 0;
		public bool HasInputActionError => inputStatus.Any(x => !x.Value.Success);

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

		public void SetValue<T>(string key, T value) {
			if (value == null) {
				throw new ArgumentException("Value cannot be null");
			}
			values[key] = value;
		}

		public void SetInputActionStatus(InputActionStatus status) {
			this.inputStatus[status.Name] = status;
		}
	}
}