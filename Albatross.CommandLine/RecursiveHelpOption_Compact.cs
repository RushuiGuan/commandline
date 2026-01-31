using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace Albatross.CommandLine {
	/// <summary>
	/// Compact layout: Shows command hierarchy with indentation, minimal parameter details.
	/// Each subcommand is indented 2 spaces deeper than its parent.
	/// Only shows option/argument names without descriptions.
	/// </summary>
	public class RecursiveHelpOption_Compact : Option<bool> {
		public RecursiveHelpOption_Compact() : base("--help-all") {
			Description = "Show help for this command and all subcommands (compact layout)";
			DefaultValueFactory = _ => false;
			this.Action = new RecursiveHelpAction(this);
		}

		private class RecursiveHelpAction : SynchronousCommandLineAction {
			private readonly RecursiveHelpOption_Compact option;

			public RecursiveHelpAction(RecursiveHelpOption_Compact option) {
				this.option = option;
			}

			public override int Invoke(ParseResult parseResult) {
				option.DisplayHelp(parseResult);
				return 0;
			}
		}

		public void DisplayHelp(ParseResult parseResult) {
			var command = parseResult.CommandResult.Command;
			var writer = new StringWriter();
			
			WriteRecursiveHelp(command, writer, 0);
			
			Console.Out.Write(writer.ToString());
		}

		private void WriteRecursiveHelp(Command command, TextWriter writer, int depth) {
			var indent = new string(' ', depth * 2);
			
			// Write command name
			var commandName = depth == 0 ? GetFullCommandName(command) : command.Name;
			writer.WriteLine($"{indent}{commandName}");
			
			// Write description if available (indented)
			if (!string.IsNullOrWhiteSpace(command.Description)) {
				writer.WriteLine($"{indent}  {command.Description}");
			}
			
			// Show options count if any exist (no details)
			var commandOptions = command.Options
				.Where(o => !o.Recursive && o != this)
				.ToList();
			
			if (commandOptions.Any()) {
				var optionNames = string.Join(", ", commandOptions.Select(o => o.Aliases.FirstOrDefault() ?? o.Name));
				writer.WriteLine($"{indent}  Options: {optionNames}");
			}
			
			// Show arguments count if any exist (no details)
			if (command.Arguments.Any()) {
				var argNames = string.Join(", ", command.Arguments.Select(a => $"<{a.Name}>"));
				writer.WriteLine($"{indent}  Arguments: {argNames}");
			}
			
			// Recursively write help for subcommands with increased indentation
			foreach (var subcommand in command.Subcommands.OrderBy(c => c.Name)) {
				WriteRecursiveHelp(subcommand, writer, depth + 1);
			}
		}

		private string GetFullCommandName(Command command) {
			if (command is RootCommand) {
				return command.Name;
			}
			
			var parts = new System.Collections.Generic.List<string>();
			var current = command;
			
			while (current != null && current is not RootCommand) {
				parts.Insert(0, current.Name);
				current = current.Parents.OfType<Command>().FirstOrDefault();
			}
			
			return string.Join(" ", parts);
		}
	}
}
