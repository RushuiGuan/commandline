using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;

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
		public static Command AddCommand<T>(this Dictionary<string, Command> dictionary, string key, Setup setup) where T : Command, new() {
			var command = new T();
			if (command is IRequireInitialization instance) {
				instance.Init();
			}
			dictionary.Add(key, command);
			ParseKey(key, out var parent, out _);
			var parentCommand = GetOrCreateHelpCommand(dictionary, parent);
			parentCommand.Add(command);
			// this step has to be done after the command has been added to its parent
			if (command.Action == null) {
				command.Action = setup.CreateGlobalCommandHandler(command);
			}
			return command;
		}

		public static bool HasParent(this ParseResult result, string command) {
			return true;
		}

		private static void GetKey(this Command command, StringBuilder sb, HashSet<Command> set) {
			if (set.Contains(command)) {
				throw new InvalidOperationException($"Circular reference detected in command {command.Name}");
			} else {
				set.Add(command);
			}
			var parent = command.Parents.FirstOrDefault();
			if (parent == null || parent is RootCommand) {
				sb.Append(command.Name);
			} else if (parent is Command parentCommand) {
				GetKey(parentCommand, sb, set);
				sb.Append(' ');
				sb.Append(command.Name);
			} else {
				throw new InvalidOperationException($"Parent of command {command.Name} is not a Command");
			}
		}

		public static string GetCommandKey(this ParseResult result) {
			var sb = new StringBuilder();
			var set = new HashSet<Command>();
			GetKey(result.CommandResult.Command, sb, set);
			return sb.ToString();
		}
	}
}