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

		public static string GetCommandKey(this Command command) {
			var parts = command.GetCommandNames();
			return string.Join(" ", parts);
		}

		public static CommandBuilder AddWithParentKey(this CommandBuilder builder, string? parentKey, Command command) {
			var fullKey = string.IsNullOrEmpty(parentKey) ? command.Name : $"{parentKey} {command.Name}";
			builder.Add(fullKey, command);
			return builder;
		}

		public static LogEventLevel Translate(this LogLevel logLevel)
			=> logLevel switch {
				LogLevel.Verbose => LogEventLevel.Verbose,
				LogLevel.Debug => LogEventLevel.Debug,
				LogLevel.Information => LogEventLevel.Information,
				LogLevel.Info => LogEventLevel.Information,
				LogLevel.Warning => LogEventLevel.Warning,
				LogLevel.Error => LogEventLevel.Error,
				LogLevel.Fatal => LogEventLevel.Fatal,
				_ => throw new NotSupportedException($"LogLevel {logLevel} is not supported."),
			};
	}
}