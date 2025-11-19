using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public static class Extensions {
		public static string ParsedCommandName(this ParseResult result) => result.CommandResult.Command.Name;
		static void ParseKey(string key, out string parent, out string self) {
			var index = key.LastIndexOf(' ');
			if (index == -1) {
				parent = string.Empty;
				self = key;
			} else {
				parent = key.Substring(0, index);
				self = key.Substring(index + 1);
			}
		}
		static Command GetOrCreateHelpCommand(Dictionary<string, Command> dictionary, string key) {
			if (dictionary.TryGetValue(key, out var command)) {
				return command;
			}
			if (key == string.Empty) {
				throw new InvalidOperationException("Dictionary is missing RootCommand");
			} else {
				ParseKey(key, out var parent, out var self);
				var parentCommand = GetOrCreateHelpCommand(dictionary, parent);
				var newCommand = new Command(self);
				newCommand.SetAction(new HelpCommandHandler().Invoke);
				parentCommand.Add(newCommand);
				dictionary[key] = newCommand;
				return newCommand;
			}
		}
		public static Command AddCommand<T>(this Dictionary<string, Command> dictionary, string key, Func<ParseResult, CancellationToken, Task<int>> globalCommandAction) where T : Command, new() {
			var command = new T();
			dictionary.Add(key, command);
			ParseKey(key, out var parent, out _);
			var parentCommand = GetOrCreateHelpCommand(dictionary, parent);
			parentCommand.Add(command);
			// this step has to be done after the command has been added to its parent
			if (command.Action == null) {
				command.SetAction(globalCommandAction);
			}
			return command;
		}

		public static bool HasParent(this ParseResult result, string command) {
			return true;
		}

		public static string[] GetCommandNames(this Command command) {
			var stack = new Stack<string>();
			do {
				stack.Push(command.Name);
			} while (command is not RootCommand);
			return stack.ToArray();
		}
	}
}