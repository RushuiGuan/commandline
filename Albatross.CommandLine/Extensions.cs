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

		public static T SetOptionAction<T, O>(this T command, Func<T, O> func, CommandHost host) where T : Command where O:Option {
			var option = func(command);
			option.Action = new AsyncOptionHandler<O>(option, host.GetServiceProvider);
			return command;
		}
	}
}