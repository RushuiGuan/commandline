using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
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
		/// <summary>
		/// The key that identifies the root command.  Register a keyed <see cref="IAsyncCommandHandler"/> under this
		/// key - or declare a verb with an empty name - to give the root command its own handler.
		/// </summary>
		public const string RootCommandKey = "";

		private readonly Dictionary<string, Command> commands = new();
		/// <summary>
		/// Gets the root command of the command hierarchy.  Mutate it directly to give the root its own options and
		/// arguments - code generation cannot, because a verb with an empty name produces no command class.
		/// </summary>
		public RootCommand RootCommand { get; }

		/// <summary>
		/// Creates a new command builder with the specified description for the root command.
		/// </summary>
		/// <param name="rootCommandDescription">The description to display in help text for the root command.</param>
		public CommandBuilder(string rootCommandDescription) {
			RootCommand = new RootCommand(rootCommandDescription);
			// The root command action is assigned by BuildTree along with every other command so that a handler
			// registered under RootCommandKey is honored.  Without a handler the global action falls back to
			// printing help, which is the behavior a bare root command had when it was wired to HelpAction here.
			commands.Add(RootCommandKey, RootCommand);
		}

		/// <summary>
		/// Adds a new command instance of the specified type to the command hierarchy.
		/// </summary>
		/// <typeparam name="T">
		/// The command type to instantiate and add.  It must expose a parameterless constructor unless
		/// <paramref name="key"/> is <see cref="RootCommandKey"/>, in which case nothing is constructed.  The
		/// constraint cannot be expressed as <c>new()</c> because <see cref="System.CommandLine.RootCommand"/>
		/// declares only a constructor with an optional parameter, which does not satisfy it.
		/// </typeparam>
		/// <param name="key">The space-separated key defining the command's position in the hierarchy.</param>
		/// <returns>
		/// The newly created command instance, or the existing <see cref="RootCommand"/> when <paramref name="key"/>
		/// is <see cref="RootCommandKey"/>.
		/// </returns>
		public T Add<
#if NET10_0_OR_GREATER
	  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
			T>(string key) where T : Command {
			// The root command is created by this class, so an empty key does not create anything - it resolves the
			// existing instance.  This is how a verb with an empty name attaches its handler to the root command.
			if (key == RootCommandKey) {
				if (RootCommand is T root) {
					return root;
				}
				throw new ArgumentException($"The root command cannot be created as '{typeof(T).FullName}'.  Use the {nameof(RootCommand)} property to configure the existing root command");
			}
			T t;
			try {
				t = Activator.CreateInstance<T>();
			} catch (MissingMethodException err) {
				throw new ArgumentException($"The command '{key}' cannot be created because '{typeof(T).FullName}' has no parameterless constructor", err);
			}
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
			if (!commands.TryGetValue(key, out var tmp)) {
				ParseCommandText(key, out var parent, out var self);
				command = new Command(self);
				command.SetAction(globalHandler);
				commands.Add(key, command);
				GetOrCreateCommand(parent, globalHandler, out var parentCommand);
				parentCommand.Add(command);
			} else {
				command = tmp;
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

		internal bool TryGetCommand(string key,
#if NET10_0_OR_GREATER
			[NotNullWhen(true)]
#endif
			out Command? command) {
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