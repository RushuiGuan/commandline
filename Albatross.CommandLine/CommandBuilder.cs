using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class CommandBuilder {
		private readonly Dictionary<string, Command> commands = new();
		public RootCommand RootCommand { get; }

		public CommandBuilder(string rootCommandDescription) {
			RootCommand = new RootCommand(rootCommandDescription) {
				new Option<LogLevel?>(VerbosityOptionName, "-v") {
					Description = "Set the logging verbosity level",
					Recursive = true,
				}
			};
			RootCommand.SetAction(HelpCommandAction.Invoke);
			commands.Add(string.Empty, RootCommand);
		}

		public void Add<T>(string key) where T : Command, new()
			=> Add(key, new T());

		public void Add<T>(string key, T command) where T : Command {
			try {
				commands.Add(key, command);
			} catch (ArgumentException) {
				throw new ArgumentException($"The command '{key}' has already been added");
			}
		}

		internal static readonly Option<string?> FormatOption = new Option<string?>(FormatOptionName) {
			Description = "Specify the optional output format expression.  See the formatting cheatsheet @https://github.com/RushuiGuan/text/blob/main/Albatross.Text.CliFormat/CheatSheet.md",
			Recursive = true,
		};

		public const string VerbosityOptionName = "--verbosity";
		public const string FormatOptionName = "--format";

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

		internal void GetOrCreateCommand(string key, Func<ParseResult, CancellationToken, Task<int>> globalHandler, out Command command) {
			if (!commands.TryGetValue(key, out command)) {
				ParseCommandText(key, out var parent, out var self);
				command = new Command(self);
				command.SetAction(globalHandler);
				commands.Add(key, command);
				GetOrCreateCommand(parent, globalHandler, out var parentCommand);
				parentCommand.Add(command);
			}
		}

		internal void AddToParentCommand(string key, Command command, Func<ParseResult, CancellationToken, Task<int>> globalHandler) {
			if (string.IsNullOrEmpty(key)) {
				throw new ArgumentException("Cannot perform AddToParentCommand action with the RootCommand");
			}
			ParseCommandText(key, out var parent, out var self);
			GetOrCreateCommand(parent, globalHandler, out var parentCommand);
			parentCommand.Add(command);
		}

		public void BuildTree(Func<IHost> hostFactory) {
			var globalCommandAction = new GlobalCommandAction(hostFactory);
			foreach (var item in this.commands.OrderBy(x => x.Key).ToArray()) {
				if (!string.IsNullOrEmpty(item.Key)) {
					AddToParentCommand(item.Key, item.Value, globalCommandAction.InvokeAsync);
				}
				if (item.Value.Action == null) {
					item.Value.SetAction(globalCommandAction.InvokeAsync);
				}
			}
		}
	}
}