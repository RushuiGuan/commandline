using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Albatross.CommandLine {
	/// <summary>
	/// Extension methods for command-line configuration and command hierarchy management.
	/// </summary>
	public static class Extensions {
		/// <summary>
		/// Gets the names of all commands in the hierarchy from root to this command.
		/// </summary>
		/// <param name="command">The command to get names for.</param>
		/// <returns>An array of command names from the root to this command, excluding the root command.</returns>
		/// <exception cref="InvalidOperationException">Thrown when a circular reference is detected in the command hierarchy.</exception>
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

		/// <summary>
		/// Gets the space-separated key that identifies this command in the hierarchy.
		/// </summary>
		/// <param name="command">The command to get the key for.</param>
		/// <returns>A space-separated string of command names from root to this command.</returns>
		public static string GetCommandKey(this Command command) {
			var parts = command.GetCommandNames();
			return string.Join(" ", parts);
		}

		/// <summary>
		/// Adds a command to the command host under the specified parent.
		/// </summary>
		/// <param name="commandHost">The command host to add the command to.</param>
		/// <param name="parentKey">The space-separated key of the parent command, or null/empty for the root.</param>
		/// <param name="command">The command to add.</param>
		/// <returns>The command host for method chaining.</returns>
		public static CommandHost AddCommand(this CommandHost commandHost, string? parentKey, Command command) {
			var fullKey = string.IsNullOrEmpty(parentKey) ? command.Name : $"{parentKey} {command.Name}";
			commandHost.CommandBuilder.Add(fullKey, command);
			return commandHost;
		}

		/// <summary>
		/// Sets an async option handler for the specified option on a command.
		/// </summary>
		/// <typeparam name="TCommand">The type of the command.</typeparam>
		/// <typeparam name="TOption">The type of the option.</typeparam>
		/// <typeparam name="THandler">The type of the option handler.</typeparam>
		/// <param name="command">The command containing the option.</param>
		/// <param name="func">A function to select the option from the command.</param>
		/// <param name="host">The command host providing the service provider.</param>
		/// <returns>The command for method chaining.</returns>
		public static TCommand SetOptionAction<TCommand, TOption, THandler>(this TCommand command, Func<TCommand, TOption> func, CommandHost host)
			where TCommand : Command
			where TOption : Option
			where THandler : IAsyncOptionHandler<TOption> {
			var option = func(command);
			option.Action = new AsyncOptionAction<TOption, THandler>(option, host.GetServiceProvider);
			return command;
		}

		/// <summary>
		/// Sets an async option handler that returns a value for the specified option on a command.
		/// The returned value is stored in the command context for use by subsequent handlers.
		/// </summary>
		/// <typeparam name="TCommand">The type of the command.</typeparam>
		/// <typeparam name="TOption">The type of the option.</typeparam>
		/// <typeparam name="THandler">The type of the option handler.</typeparam>
		/// <typeparam name="TValue">The type of the value returned by the handler.</typeparam>
		/// <param name="command">The command containing the option.</param>
		/// <param name="func">A function to select the option from the command.</param>
		/// <param name="host">The command host providing the service provider.</param>
		/// <returns>The command for method chaining.</returns>
		public static TCommand SetOptionAction<TCommand, TOption, THandler, TValue>(this TCommand command, Func<TCommand, TOption> func, CommandHost host)
			where TCommand : Command
			where TOption : Option
			where THandler : IAsyncOptionHandler<TOption, TValue>
			where TValue : notnull {
			var option = func(command);
			option.Action = new AsyncOptionAction<TOption, THandler, TValue>(option, host.GetServiceProvider);
			return command;
		}
	}
}