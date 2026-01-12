using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Albatross.CommandLine.Test")]
namespace Albatross.CommandLine {
	/// <summary>
	/// Builds and manages a hierarchical command structure for command-line interfaces.
	/// Commands are organized by space-separated keys (e.g., "parent child") that define the hierarchy.
	/// </summary>
	public class CommandBuilder {
		private readonly Dictionary<string, Command> commands = new();
		/// <summary>
		/// Gets the root command of the command hierarchy.
		/// </summary>
		public RootCommand RootCommand { get; }

		/// <summary>
		/// Creates a new command builder with the specified description for the root command.
		/// The root command includes a global verbosity option by default.
		/// </summary>
		/// <param name="rootCommandDescription">The description to display in help text for the root command.</param>
		public CommandBuilder(string rootCommandDescription) {
			RootCommand = new RootCommand(rootCommandDescription) {
				VerbosityOption,
			};
			RootCommand.SetAction(new HelpAction().Invoke);
			commands.Add(string.Empty, RootCommand);
		}

		/// <summary>
		/// Adds a new command instance of the specified type to the command hierarchy.
		/// </summary>
		/// <typeparam name="T">The command type to instantiate and add.</typeparam>
		/// <param name="key">The space-separated key defining the command's position in the hierarchy.</param>
		/// <returns>The newly created command instance.</returns>
		public T Add<T>(string key) where T : Command, new() {
			var t = new T();
			Add(key, t);
			return t;
		}

		/// <summary>
		/// Adds an existing command to the command hierarchy.
		/// </summary>
		/// <typeparam name="T">The command type.</typeparam>
		/// <param name="key">The space-separated key defining the command's position in the hierarchy.</param>
		/// <param name="command">The command instance to add.</param>
		/// <exception cref="ArgumentException">Thrown when a command with the same key already exists.</exception>
		public void Add<T>(string key, T command) where T : Command {
			try {
				commands.Add(key, command);
			} catch (ArgumentException) {
				throw new ArgumentException($"The command '{key}' has already been added");
			}
		}

		/// <summary>
		/// Gets the global verbosity option shared across all commands.
		/// </summary>
		public static VerbosityOption VerbosityOption { get; } = new() {
			Required = false,
			Recursive = true,
		};

		/// <summary>
		/// Parse the command text and return the immediate (last) sub command and its complete parent command
		/// if the text is "a b c", it will return "c" as self and "a b" as parent
		/// </summary>
		/// <param name="commandText"></param>
		/// <param name="parent"></param>
		/// <param name="self"></param>
		public static void ParseCommandText(string commandText, out string parent, out string self) {
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

		internal bool TryGetCommand(string key, out Command command) {
			return commands.TryGetValue(key, out command);
		}

		/// <summary>
		/// Builds the complete command tree by linking child commands to their parents and setting up command actions.
		/// This method should be called after all commands have been added and before parsing.
		/// </summary>
		/// <param name="serviceFactory">A factory function that provides the service provider for dependency injection.</param>
		public void BuildTree(Func<IServiceProvider> serviceFactory) {
			var action = new GlobalCommandAction(serviceFactory);
			// ordering is required here to ensure parent commands are created before child commands
			// ordering cannot be done in code generation because commands can be added manually
			foreach (var item in this.commands.OrderBy(x => x.Key).ToArray()) {
				if (!string.IsNullOrEmpty(item.Key)) {
					AddToParentCommand(item.Key, item.Value, action.InvokeAsync);
				}
				if (item.Value.Action == null) {
					item.Value.SetAction(action.InvokeAsync);
				}
			}
		}
	}
}