using Albatross.Text.CliFormat;
using Albatross.Text.CliFormat.Operations;
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
	/// A command-line option that displays help for a command and all of its subcommands recursively.
	/// This is an optional option that can be added to any command as needed.
	/// Supports "compact" (default) and "detailed" modes.
	/// Uses a data structure approach for organizing help information before rendering.
	/// </summary>
	public class RecursiveHelpOption : Option<string> {
		public const string CompactMode = "compact";
		public const string DetailedMode = "detailed";
		/// <summary>
		/// Creates a new recursive help option with alias --help-all.
		/// </summary>
		public RecursiveHelpOption() : base("--help-all") {
			Description = "Show help for this command and all subcommands (compact|detailed)";
			Required = false;
			DefaultValueFactory = _ => CompactMode;
			this.CompletionSources.Add(_ => [CompactMode, DetailedMode]);
			this.Action = new ActionHandler(this);
		}

		private class ActionHandler : SynchronousCommandLineAction {
			private readonly Option<string> option;
			public ActionHandler(Option<string> option) {
				this.option = option;
			}
			public override bool Terminating => true;
			public override int Invoke(ParseResult parseResult) {
				var model = parseResult.GetValue(option);
				var data = BuildHelpData(parseResult);
				var tableOptions = new TableOptions<HelpData>();
				if (model == CompactMode) {
				} else {
				}
				data.StringTable(tableOptions).Print(parseResult.InvocationConfiguration.Output);
				return 0;
			}
		}


		private static IEnumerable<HelpData> BuildHelpData(Command command) {
		}

		private record class HelpData {
			public required string CommandName { get; set; }
			public string? CommandDescription { get; set; }
			public string? Aliases { get; set; }
			public IEnumerable<ParameterInfo> Parameters { get; set; } = Enumerable.Empty<ParameterInfo>();
		}

		private class ParameterInfo {
			public string Aliases { get; set; } = string.Empty;
			public string? Description { get; set; }
			public string? Name { get; set; }
		}
	}
}