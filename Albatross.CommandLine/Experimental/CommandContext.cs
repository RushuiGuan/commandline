using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Albatross.CommandLine {
	/// <summary>
	/// This interface is used to create a context bewteen the Option\Argument actions to Command action
	/// </summary>
	public interface ICommandContext {
		string Key { get; }

		T? GetReferenceValue<T>(string key) where T : class;
		T? GetStructValue<T>(string key) where T : struct;
		void SetValue<T>(string key, T value) where T : notnull;
		void SetInputActionStatus(InputActionStatus status);
		bool HasParsingError { get; }
		bool HasInputActionError { get; }
	}

	public class CommandContext : ICommandContext {
		public string Key { get; }
		private readonly ParseResult result;
		private readonly Dictionary<string, object> values = new();
		private readonly Dictionary<string, InputActionStatus> inputStatus = new();

		public bool HasParsingError => result.Errors.Count > 0;
		public bool HasInputActionError => inputStatus.Any(x => !x.Value.Success);

		public CommandContext(ParseResult result) {
			this.result = result;
			this.Key = result.CommandResult.Command.GetCommandKey();
		}

		public T? GetReferenceValue<T>(string key) where T : class {
			if (values.TryGetValue(key, out var value)) {
				if (value is T t) {
					return t;
				} else {
					throw new InvalidOperationException($"Stored value for key {key} is not of type {typeof(T).FullName}");
				}
			} else {
				return null;
			}
		}

		public T? GetStructValue<T>(string key) where T : struct {
			if (values.TryGetValue(key, out var value)) {
				if (value is T t) {
					return t;
				} else {
					throw new InvalidOperationException($"Stored value for key {key} is not of type {typeof(T).FullName}");
				}
			} else {
				return null;
			}
		}

		public void SetValue<T>(string key, T value) where T : notnull {
			if (value == null) {
				throw new ArgumentException("Value cannot be null");
			}
			values[key] = value!;
		}

		public void SetInputActionStatus(InputActionStatus status) {
			this.inputStatus[status.Name] = status;
		}
	}
}