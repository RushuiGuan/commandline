using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;

namespace Albatross.CommandLine {
	public static class Extensions {
		public static string ParsedCommandName(this InvocationContext context) => context.ParseResult.CommandResult.Command.Name;

		public static string? Prompt(this string message) {
			Console.Write(message);
			return Console.ReadLine();
		}
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
				var newCommand = new Command(self) {
					Handler = new HelpCommandHandler(),
				};
				parentCommand.AddCommand(newCommand);
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
			parentCommand.AddCommand(command);
			// this step has to be done after the command has been added to its parent
			if (command.Handler == null) {
				command.Handler = setup.CreateGlobalCommandHandler(command);
			}
			return command;
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
		public static string GetKey(this Command command) {
			var sb = new System.Text.StringBuilder();
			var set = new HashSet<Command>();
			GetKey(command, sb, set);
			return sb.ToString();
		}
	}
}