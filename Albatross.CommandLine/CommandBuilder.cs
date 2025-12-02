using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

namespace Albatross.CommandLine {
	public class CommandBuilder {
		Dictionary<string, Command> commands = new();

		public RootCommand RootCommand { get; }
		public CommandBuilder(string rootCommandDescription) {
			RootCommand = new RootCommand(rootCommandDescription);
			RootCommand.SetAction(HelpCommandHandler.Invoke);
			AddVerbosityOption(RootCommand);
			commands.Add(string.Empty, RootCommand);
		}
		public void Add<T>(string commandText) where T : Command, new()
			=> Add(commandText, new T());

		public void Add<T>(string commandText, T command) where T : Command {
			try {
				commands.Add(commandText, command);
			} catch (ArgumentException) {
				throw new ArgumentException($"The command '{commandText}' has already been added");
			}
		}

		internal static readonly Option<string?> FormatOption = new Option<string?>(FormatOptionName) {
			Description = "Specify the optional output format expression.  See the formatting cheatsheet @https://github.com/RushuiGuan/text/blob/main/Albatross.Text.CliFormat/CheatSheet.md"
		};

		internal static readonly Option<bool> BenchmarkOption = new Option<bool>(BenchmarkOptionName) {
			Description = "Show the time it takes to run the command in milliseconds"
		};

		internal static readonly Option<bool> ShowStackOption = new Option<bool>(ShowStackOptionName) {
			Description = "Show the full stack when an exception has been thrown"
		};

		public const string VerbosityOptionName = "--verbosity";
		public const string FormatOptionName = "--format";
		public const string BenchmarkOptionName = "--benchmark";
		public const string ShowStackOptionName = "--show-stack";
		void AddVerbosityOption(Command command){
			var allowedValues = new[] { "Verbose", "Debug", "Information", "Info", "Warning", "Error", "Err", "Fatal" };
			var option = new Option<string?>(VerbosityOptionName, "-v") {
				Description = "Set the verbosity level of logging",
				DefaultValueFactory = _ => "Error",
			};
			option.CompletionSources.Add(allowedValues);
			option.Validators.Add(result => {
				var value = result.GetValue<string?>(VerbosityOptionName);
				if (value != null && !allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase)) {
					result.AddError($"Invalid verbosity level '{value}'. Allowed values are: {string.Join(", ", allowedValues)}");
				}
			});
			command.Add(option);
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

		internal void GetOrCreateCommand(string key, out Command command) {
			if (!commands.TryGetValue(key, out command)) {
				ParseCommandText(key, out var parent, out var self);
				command = new Command(self);
				command.SetAction(HelpCommandHandler.Invoke);
				commands[key] = command;
				GetOrCreateCommand(parent, out var parentCommand);
				parentCommand.Add(command);
			}
		}

		internal void AddToParentCommand(string key, Command command) {
			if (string.IsNullOrEmpty(key)) {
				throw new ArgumentException("Cannot perform AddToParentCommand action with the RootCommand");
			}
			ParseCommandText(key, out var parent, out var self);
			GetOrCreateCommand(parent, out var parentCommand);
			parentCommand.Add(command);
		}

		public void Build(IHost host) {
			var globalCommandAction = new GlobalCommandAction(host);
			foreach (var item in this.commands) {
				if (!string.IsNullOrEmpty(item.Key)) {
					AddToParentCommand(item.Key, item.Value);
				}
				if (item.Value.Action == null) {
					item.Value.SetAction(globalCommandAction.InvokeAsync);
				}
			}
		}
	}
}
