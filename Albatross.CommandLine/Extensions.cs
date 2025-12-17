using Serilog.Events;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Albatross.CommandLine {
	public static class Extensions {
		public static string[] GetCommandNames(this Command command) {
			var stack = new Stack<string>();
			var hashSet = new HashSet<Command>();
			for (var current = command; current is not RootCommand && current != null; current = current.Parents.FirstOrDefault() as Command) {
				if (hashSet.Contains(current)) {
					throw new InvalidOperationException("Circular reference detected in command hierarchy.");
				} else {
					hashSet.Add(current);
				}
				stack.Push(current.Name);
			}
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