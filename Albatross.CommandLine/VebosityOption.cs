using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Linq;

namespace Albatross.CommandLine {
	/// <summary>
	/// A command-line option for controlling logging verbosity level.
	/// Supports case-insensitive prefix matching (e.g., "v" matches "Verbose", "d" matches "Debug").
	/// </summary>
	public class VerbosityOption : Option<string> {
		/// <summary>Maps to <see cref="LogLevel.Trace"/>.</summary>
		public const string Verbose = "Verbose";
		/// <summary>Maps to <see cref="LogLevel.Debug"/>.</summary>
		public const string Debug = "Debug";
		/// <summary>Maps to <see cref="LogLevel.Information"/>.</summary>
		public const string Info = "Info";
		/// <summary>Maps to <see cref="LogLevel.Warning"/>.</summary>
		public const string Warning = "Warning";
		/// <summary>Maps to <see cref="LogLevel.Error"/>.</summary>
		public const string Error = "Error";
		/// <summary>Maps to <see cref="LogLevel.Critical"/>.</summary>
		public const string Critical = "Critical";
		/// <summary>Maps to <see cref="LogLevel.None"/>.</summary>
		public const string None = "None";

		/// <summary>
		/// Creates a new verbosity option with aliases --verbosity and -v.
		/// Defaults to Error level.
		/// </summary>
		public VerbosityOption() : base("--verbosity", "-v") {
			var allowedValues = new[] { Verbose, Debug, Info, Warning, Error, Critical, None };
			DefaultValueFactory = _ => Error;
			this.CustomParser = (result) => {
				if (result.Tokens.Count != 1) {
					result.AddError("Verbosity option requires a single value.");
					return null;
				}
				var value = result.Tokens.Single().Value;
				for (int i = 0; i < allowedValues.Length; i++) {
					if (allowedValues[i].StartsWith(value, StringComparison.InvariantCultureIgnoreCase)) {
						return allowedValues[i];
					}
				}
				result.AddError($"Invalid verbosity level '{value}'. Value must be a case insensitive prefix of: Verbose, Debug, Info, Warning, Error, Critical, or None.");
				return null;
			}
				;
			this.CompletionSources.Add(_ => allowedValues);
		}

		/// <summary>
		/// Converts the verbosity option value from the parse result to a <see cref="LogLevel"/>.
		/// </summary>
		/// <param name="result">The parse result containing the option value.</param>
		/// <returns>The corresponding log level, or <see cref="LogLevel.Error"/> if parsing failed.</returns>
		public LogLevel GetLogLevel(ParseResult result) {
			var argumentResult = result.CommandResult.GetResult(this);
			if (argumentResult == null || argumentResult.Errors.Any()) {
				// if there is any error, return Error level
				return LogLevel.Error;
			}
			var value = result.GetValue(this);
			return value switch {
				Verbose => LogLevel.Trace,
				Debug => LogLevel.Debug,
				Info => LogLevel.Information,
				Warning => LogLevel.Warning,
				Error => LogLevel.Error,
				Critical => LogLevel.Critical,
				None => LogLevel.None,
				_ => LogLevel.Error,
			};
		}
	}
}
