using Albatross.Text.Table;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace Albatross.CommandLine.Inputs {
	/// <summary>
	/// An optional command-line option that displays help for a command and all of its subcommands recursively.
	/// Add it to any command or to the root command with <see cref="Option.Recursive"/> = true to make it available everywhere.
	/// Usage: <c>app --help-all</c> (compact) or <c>app --help-all detailed</c> (with options/arguments per command).
	/// </summary>
	public class RecursiveHelpOption : Option<string> {
		public const string CompactMode = "compact";
		public const string DetailedMode = "detailed";

		public RecursiveHelpOption() : base("--help-all") {
			Description = "Show help for this command and all subcommands";
			Arity = ArgumentArity.ZeroOrOne;
			this.CustomParser = result => result.Tokens.Count == 0
				? CompactMode
				: result.Tokens[0].Value.Equals(DetailedMode, StringComparison.OrdinalIgnoreCase)
					? DetailedMode
					: CompactMode;
			this.CompletionSources.Add(_ => new[] { CompactMode, DetailedMode });
			this.Action = new ActionHandler(this);
		}

		private class ActionHandler : SynchronousCommandLineAction {
			private readonly RecursiveHelpOption option;
			public override bool Terminating => true;

			public ActionHandler(RecursiveHelpOption option) {
				this.option = option;
			}

			public override int Invoke(ParseResult parseResult) {
				var mode = parseResult.GetValue(option) ?? CompactMode;
				var command = parseResult.CommandResult.Command;
				var writer = parseResult.InvocationConfiguration.Output;
				if (mode.Equals(DetailedMode, StringComparison.OrdinalIgnoreCase)) {
					PrintDetailed(command, writer);
				} else {
					PrintCompact(command, writer);
				}
				return 0;
			}
		}

		/// <summary>
		/// Prints a two-column table of all commands with their descriptions,
		/// truncating to fit the console width.
		/// </summary>
		private static void PrintCompact(Command root, TextWriter writer) {
			var table = new StringTable("Command", "Description");
			foreach (var command in GetCommandsToShow(root)) {
				table.AddRow(GetFullName(command), command.Description ?? string.Empty);
			}
			table.PrintConsole(writer, StringTableExtensions.GetConsoleWith());
		}

		/// <summary>
		/// Prints a block per command showing description and a table of options/arguments.
		/// </summary>
		private static void PrintDetailed(Command root, TextWriter writer) {
			var separator = new string('-', Math.Min(StringTableExtensions.GetConsoleWith(), 60));
			bool first = true;
			foreach (var command in GetCommandsToShow(root)) {
				if (!first) { writer.WriteLine(separator); }
				first = false;
				PrintCommandBlock(command, writer);
			}
		}

		private static void PrintCommandBlock(Command command, TextWriter writer) {
			writer.WriteLine(GetFullName(command));
			if (!string.IsNullOrWhiteSpace(command.Description)) {
				writer.WriteLine($"  {command.Description}");
			}

			var options = command.Options.Where(o => !o.Recursive).ToList();
			if (options.Count > 0) {
				writer.WriteLine();
				writer.WriteLine("  Options:");
				foreach (var opt in options) {
					var aliases = string.Join(", ", new[] { opt.Name }.Concat(opt.Aliases));
					var valueHint = opt.ValueType == typeof(bool) ? string.Empty : $" <{opt.Name.TrimStart('-')}>";
					writer.WriteLine($"    {aliases}{valueHint}");
					if (!string.IsNullOrWhiteSpace(opt.Description)) {
						writer.WriteLine($"        {opt.Description}");
					}
				}
			}

			var arguments = command.Arguments.ToList();
			if (arguments.Count > 0) {
				writer.WriteLine();
				writer.WriteLine("  Arguments:");
				foreach (var arg in arguments) {
					writer.WriteLine($"    <{arg.Name}>");
					if (!string.IsNullOrWhiteSpace(arg.Description)) {
						writer.WriteLine($"        {arg.Description}");
					}
				}
			}
		}

		/// <summary>
		/// Returns commands to display: skips the root command itself (not meaningful),
		/// but includes non-root commands along with all their descendants.
		/// </summary>
		private static IEnumerable<Command> GetCommandsToShow(Command root)
			=> root is RootCommand
				? root.Subcommands.OrderBy(c => c.Name).SelectMany(GetSelfAndDescendants)
				: GetSelfAndDescendants(root);

		/// <summary>
		/// Depth-first traversal: yields the command itself, then all descendants ordered by name.
		/// </summary>
		private static IEnumerable<Command> GetSelfAndDescendants(Command command) {
			yield return command;
			foreach (var sub in command.Subcommands.OrderBy(c => c.Name)) {
				foreach (var desc in GetSelfAndDescendants(sub)) {
					yield return desc;
				}
			}
		}

		/// <summary>
		/// Returns the full invocation path for a command (e.g. "config set").
		/// </summary>
		private static string GetFullName(Command command) {
			if (command is RootCommand) {
				return command.Name;
			}
			var parts = new List<string>();
			var current = command;
			while (current != null && current is not RootCommand) {
				parts.Insert(0, current.Name);
				current = current.Parents.OfType<Command>().FirstOrDefault();
			}
			return string.Join(" ", parts);
		}
	}
}
