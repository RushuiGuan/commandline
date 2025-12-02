using Serilog.Events;
using System;
using System.Collections.Generic;
using System.CommandLine;

namespace Albatross.CommandLine {
	public static class Extensions {
		public static string[] GetCommandNames(this Command command) {
			var stack = new Stack<string>();
			do {
				stack.Push(command.Name);
			} while (command is not RootCommand);
			return stack.ToArray();
		}

		public static LogEventLevel? GetLogEventLevel(string? verbosity) {
			if (string.IsNullOrEmpty(verbosity)) {
				return null;
			}
			if (Enum.TryParse<LogEventLevel>(verbosity, true, out var level)) {
				return level;
			}
			switch (verbosity.ToLowerInvariant()) {
				case "err":
					return LogEventLevel.Error;
				case "info":
					return LogEventLevel.Information;
				default:
					throw new ArgumentException($"Invalid verbosity level: {verbosity}");
			}
		}
	}
}