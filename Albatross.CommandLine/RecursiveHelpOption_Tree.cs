using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;

namespace Albatross.CommandLine {
	/// <summary>
	/// Tree layout: Shows command hierarchy using tree-style characters (├── and └──).
	/// Uses visual tree indicators to show parent-child relationships.
	/// Shows parameter counts rather than full details.
	/// </summary>
	public class RecursiveHelpOption_Tree : Option<bool> {
		public RecursiveHelpOption_Tree() : base("--help-all") {
			Description = "Show help for this command and all subcommands (tree layout)";
			DefaultValueFactory = _ => false;
			this.Action = new RecursiveHelpAction(this);
		}

		private class RecursiveHelpAction : SynchronousCommandLineAction {
			private readonly RecursiveHelpOption_Tree option;

			public RecursiveHelpAction(RecursiveHelpOption_Tree option) {
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
			
			// Write root command name
			writer.WriteLine(GetFullCommandName(command));
			if (!string.IsNullOrWhiteSpace(command.Description)) {
				writer.WriteLine($"  {command.Description}");
			}
			WriteParameterSummary(command, writer, "");
			
			// Write subcommands as tree
			var subcommands = command.Subcommands.OrderBy(c => c.Name).ToList();
			for (int i = 0; i < subcommands.Count; i++) {
				bool isLast = i == subcommands.Count - 1;
				WriteTreeNode(subcommands[i], writer, "", isLast);
			}
			
			Console.Out.Write(writer.ToString());
		}

		private void WriteTreeNode(Command command, TextWriter writer, string prefix, bool isLast) {
			var connector = isLast ? "└── " : "├── ";
			writer.WriteLine($"{prefix}{connector}{command.Name}");
			
			var newPrefix = prefix + (isLast ? "    " : "│   ");
			
			if (!string.IsNullOrWhiteSpace(command.Description)) {
				writer.WriteLine($"{newPrefix}{command.Description}");
			}
			
			WriteParameterSummary(command, writer, newPrefix);
			
			var subcommands = command.Subcommands.OrderBy(c => c.Name).ToList();
			for (int i = 0; i < subcommands.Count; i++) {
				bool isLastSub = i == subcommands.Count - 1;
				WriteTreeNode(subcommands[i], writer, newPrefix, isLastSub);
			}
		}

		private void WriteParameterSummary(Command command, TextWriter writer, string prefix) {
			var commandOptions = command.Options
				.Where(o => !o.Recursive && o != this)
				.ToList();
			
			if (commandOptions.Any()) {
				writer.WriteLine($"{prefix}Options: {commandOptions.Count} option(s)");
			}
			
			if (command.Arguments.Any()) {
				writer.WriteLine($"{prefix}Arguments: {command.Arguments.Count} argument(s)");
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
