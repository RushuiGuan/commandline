using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace Albatross.CommandLine {
	/// <summary>
	/// Hierarchical layout: Shows full command paths with indented child commands.
	/// Each level shows only option/argument signatures without descriptions.
	/// Uses full command path for easy copy-paste while showing hierarchy.
	/// </summary>
	public class RecursiveHelpOption_Hierarchical : Option<bool> {
		public RecursiveHelpOption_Hierarchical() : base("--help-all") {
			Description = "Show help for this command and all subcommands (hierarchical layout)";
			DefaultValueFactory = _ => false;
			this.Action = new RecursiveHelpAction(this);
		}

		private class RecursiveHelpAction : SynchronousCommandLineAction {
			private readonly RecursiveHelpOption_Hierarchical option;

			public RecursiveHelpAction(RecursiveHelpOption_Hierarchical option) {
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
			
			// Write full command path
			var commandName = GetFullCommandName(command);
			writer.WriteLine($"{indent}{commandName}");
			
			// Write description if available
			if (!string.IsNullOrWhiteSpace(command.Description)) {
				writer.WriteLine($"{indent}  {command.Description}");
			}
			
			// Write options (signature only, no descriptions)
			var commandOptions = command.Options
				.Where(o => !o.Recursive && o != this)
				.ToList();
			
			if (commandOptions.Any()) {
				writer.Write($"{indent}  Options: ");
				var signatures = commandOptions.Select(o => {
					var aliases = string.Join(", ", o.Aliases);
					var valueHint = GetValueHint(o);
					return string.IsNullOrWhiteSpace(valueHint) ? aliases : $"{aliases} {valueHint}";
				});
				writer.WriteLine(string.Join(" | ", signatures));
			}
			
			// Write arguments (signature only, no descriptions)
			if (command.Arguments.Any()) {
				writer.Write($"{indent}  Arguments: ");
				var argSigs = command.Arguments.Select(a => $"<{a.Name}>");
				writer.WriteLine(string.Join(" ", argSigs));
			}
			
			// Add spacing between commands
			if (command.Subcommands.Any()) {
				writer.WriteLine();
			}
			
			// Recursively write help for subcommands with increased depth
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

		private string GetValueHint(Option option) {
			var valueType = option.ValueType;
			if (valueType == typeof(bool)) {
				return string.Empty;
			}
			
			var parameterName = option.Name.TrimStart('-');
			return $"<{parameterName}>";
		}
	}
}
