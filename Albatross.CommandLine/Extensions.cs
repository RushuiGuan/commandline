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

		public static CommandHost AddCommand(this CommandHost commandHost, string? parentKey, Command command) {
			var fullKey = string.IsNullOrEmpty(parentKey) ? command.Name : $"{parentKey} {command.Name}";
			commandHost.CommandBuilder.Add(fullKey, command);
			return commandHost;
		}
		
		public static T GetRequiredReferenceValue<T>(this ICommandContext context, string key) where T : class {
			var value = context.GetReferenceValue<T>(key);
			if (value == null) {
				throw new InvalidOperationException($"Required reference type value for key {key} is not set.");
			}
			return value;
		}
		public static T GetRequiredStructValue<T>(this ICommandContext context, string key) where T : struct {
			var value = context.GetStructValue<T>(key);
			if (value == null) {
				throw new InvalidOperationException($"Required struct type value for key {key} is not set.");
			}
			return value.Value;
		}
	}
}