using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace TestConsole;

internal class Program {
	static async Task<int> Main(string[] args) {
		// Setup DI container
		var services = new ServiceCollection();
		services.AddLogging(builder => {
			builder.AddConsole();
			builder.SetMinimumLevel(LogLevel.Information);
		});
		services.AddTransient<IDemoService, DemoService>();

		using var serviceProvider = services.BuildServiceProvider();

		// Create root command with comprehensive structure
		var rootCommand = new RootCommand("System.CommandLine 2.0.0 Demonstration Program");

		// Add global options to demonstrate global option functionality
		var verboseGlobalOption = new Option<bool>("--verbose") {
			Description = "Enable verbose output"
		};
		rootCommand.Add(verboseGlobalOption);

		var debugGlobalOption = new Option<bool>("--debug") {
			Description = "Enable debug mode"
		};
		rootCommand.Add(debugGlobalOption);
		rootCommand.Add(CreateGreetCommand());
		rootCommand.Add(CreatEmptyCommand());
		rootCommand.SetAction(GlobalInvoke);
		await rootCommand.Parse(args).InvokeAsync();
		return 0;
	}

	private static int GlobalInvoke(ParseResult result) {
		System.Console.WriteLine("global");
		return 0;
	}

	private static Command CreateGreetCommand() {
		var command = new Command("greet", "Generate personalized greetings with various options");

		// Required string argument
		var nameArgument = new Argument<string>("name") {
			Description = "The name of the person to greet"
		};
		command.Add(nameArgument);

		// Integer option with default
		var countOption = new Option<int>("--count") {
			Description = "Number of times to repeat the greeting"
		};
		command.Add(countOption);

		// Boolean flag option
		var uppercaseOption = new Option<bool>("--uppercase") {
			Description = "Convert greeting to uppercase"
		};
		command.Add(uppercaseOption);

		// String option with specific values
		var languageOption = new Option<string>("--language") {
			Description = "Language for greeting (english, spanish, french, german)"
		};
		command.Add(languageOption);

		// Enum-style option
		var styleOption = new Option<string>("--style") {
			Description = "Greeting style (formal, casual, friendly)"
		};
		command.Add(styleOption);

		command.SetAction(new GreetCommandHandler().Invoke);
		return command;
	}

	private static Command CreatEmptyCommand() {
		var command = new Command("file", "this command has no action");

		var inputFileArgument = new Argument<FileInfo?>("input-file") {
			Arity = ArgumentArity.ZeroOrOne,
			Description = "Input file to process"
		};
		command.Add(inputFileArgument);

		// Options
		var outputFileOption = new Option<FileInfo?>("--output") {
			Required = false,
			Description = "Output file (if not specified, writes to console)"
		};
		command.Add(outputFileOption);

		var formatOption = new Option<string?>("--format") {
			Required = true,
			Description = "Output format (json, xml, text)"
		};
		command.Add(formatOption);
		command.SetAction(new DefaultAction().Invoke);
		return command;
	}
}