using Microsoft.Extensions.Hosting;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class CommandBuilder {
		SortedDictionary<string, Command> commands = new();

		public RootCommand RootCommand { get; }
		public CommandBuilder(string rootCommandDescription) {
			RootCommand = new RootCommand(rootCommandDescription) {
				new Option<LogEventLevel?>("--verbosity", "-v") {
					Description = "Set the verbosity level of logging",
					DefaultValueFactory = _ => LogEventLevel.Error,
				},
				new Option<bool>("--benchmark") {
					Description = "Show the time it takes to run the command in milliseconds"
				},
				new Option<bool>("--show-stack"){
					Description = "Show the full stack when an exception has been thrown"
				},
				new Option<string?>("--format") {
					Description = "Specify the optional output format expression.  See the formatting cheatsheet @https://github.com/RushuiGuan/text/blob/main/Albatross.Text.CliFormat/CheatSheet.md"
				}
			};
		}
		public void Add<T>(string commandText) where T : Command, new()
			=> Add(commandText, new T());

		public void Add<T>(string commandText, T command) where T : Command {
			try {
				commands.Add(commandText, command);
			}catch(ArgumentException) {
				throw new ArgumentException($"The command '{commandText}' has already been added");
			}
		}

		/// <summary>
		/// Parse the command text and return the immediate (last) sub command and its complete parent command
		/// if the text is "a b c", it will return "c" as self and "a b" as parent
		/// </summary>
		/// <param name="commandText"></param>
		/// <param name="parent"></param>
		/// <param name="self"></param>
		public void ParseCommandText(string commandText, out string parent, out string self) {
			var index = commandText.LastIndexOf(' ');
			if (index == -1) {
				parent = string.Empty;
				self = commandText;
			} else {
				parent = commandText.Substring(0, index);
				self = commandText.Substring(index + 1);
			}
		}
		Command GetOrCreateCommand(Dictionary<string, Command> commands, string commandText) {
			if (commands.TryGetValue(commandText, out var command)) {
				return command;
			}
			if (commandText == string.Empty) {
				// if the commandText is empty string, it should have been returned by the dictionary.  Since only RootCommand can have empty
				// command name and it cannot be created by this function.
				throw new InvalidOperationException("The dictionary doesn't contain the RootCommand");
			} else {
				ParseCommandText(commandText, out var parent, out var self);
				var parentCommand = GetOrCreateCommand(commands, parent);
				var newCommand = new Command(self);
				newCommand.SetAction(HelpCommandHandler.Invoke);
				parentCommand.Add(newCommand);
				commands[commandText] = newCommand;
				return newCommand;
			}
		}

		public void Build(IHost host) {
			var globalCommandAction = new GlobalCommandAction(host);
			foreach (var item in this.commands) {
				item.Value.SetAction(globalCommandAction.InvokeAsync);
			}
		}
	}
}
