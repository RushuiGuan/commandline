using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using Albatross.CommandLine;

namespace RecursiveHelpLayoutDemo {
	/// <summary>
	/// Demo program to compare different RecursiveHelpOption layout styles.
	/// Run with --layout=compact, --layout=tree, or --layout=hierarchical
	/// </summary>
	class Program {
		static int Main(string[] args) {
			// Determine which layout to use
			string layout = "compact"; // default
			if (args.Length > 0 && args[0].StartsWith("--layout=")) {
				layout = args[0].Substring("--layout=".Length);
				args = args.Skip(1).ToArray();
			}

			// Create a sample command hierarchy
			var rootCommand = new RootCommand("Sample Application");
			
			// Add the appropriate help option based on layout choice
			switch (layout.ToLower()) {
				case "compact":
					rootCommand.Add(new RecursiveHelpOption_Compact { Recursive = true });
					break;
				case "tree":
					rootCommand.Add(new RecursiveHelpOption_Tree { Recursive = true });
					break;
				case "hierarchical":
					rootCommand.Add(new RecursiveHelpOption_Hierarchical { Recursive = true });
					break;
				default:
					Console.WriteLine($"Unknown layout: {layout}. Use compact, tree, or hierarchical.");
					return 1;
			}

			// Build sample command hierarchy
			var configCmd = new Command("config", "Configure application settings");
			var configSetCmd = new Command("set", "Set a configuration value");
			var keyOpt1 = new Option<string>("--key") { Description = "The configuration key" };
			var valueOpt = new Option<string>("--value") { Description = "The value to set" };
			configSetCmd.Add(keyOpt1);
			configSetCmd.Add(valueOpt);
			
			var configGetCmd = new Command("get", "Get a configuration value");
			var keyOpt2 = new Option<string>("--key") { Description = "The configuration key" };
			configGetCmd.Add(keyOpt2);
			
			var configListCmd = new Command("list", "List all configuration values");
			var formatOpt = new Option<string>("--format") { Description = "Output format (json|table)" };
			configListCmd.Add(formatOpt);
			
			configCmd.Add(configSetCmd);
			configCmd.Add(configGetCmd);
			configCmd.Add(configListCmd);
			rootCommand.Add(configCmd);

			var projectCmd = new Command("project", "Project management commands");
			var projectCreateCmd = new Command("create", "Create a new project");
			var nameOpt = new Option<string>("--name") { Description = "Project name" };
			var templateOpt = new Option<string>("--template") { Description = "Project template" };
			projectCreateCmd.Add(nameOpt);
			projectCreateCmd.Add(templateOpt);
			var pathArg = new Argument<string>("path") { Description = "Project directory path" };
			projectCreateCmd.Add(pathArg);
			
			var projectDeleteCmd = new Command("delete", "Delete a project");
			var forceOpt = new Option<bool>("--force") { Description = "Force delete without confirmation" };
			projectDeleteCmd.Add(forceOpt);
			var nameArg = new Argument<string>("name") { Description = "Project name" };
			projectDeleteCmd.Add(nameArg);
			
			projectCmd.Add(projectCreateCmd);
			projectCmd.Add(projectDeleteCmd);
			rootCommand.Add(projectCmd);

			// Parse and invoke
			return rootCommand.Parse(args).Invoke();
		}
	}
}
