using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Linq;

namespace Albatross.CommandLine {
	public class VerbosityOption : Option<string> {
		public const string Verbose = "Verbose";
		public const string Debug = "Debug";
		public const string Info = "Info";
		public const string Warning = "Warning";
		public const string Error = "Error";
		public const string Critical = "Critical";
		public const string None = "None";


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
