using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class TestRecursiveHelpOption {
		[Fact]
		public void TestRecursiveHelpOptionExists() {
			var builder = new CommandBuilder("Test Application");
			Assert.NotNull(CommandBuilder.RecursiveHelpOption);
			Assert.Contains(CommandBuilder.RecursiveHelpOption, builder.RootCommand.Options);
		}

		[Fact]
		public void TestRecursiveHelpDisplaysAllCommands() {
			// Arrange
			var builder = new CommandBuilder("Test Application");
			var cmd1 = new Command("cmd1", "Command 1 description");
			var cmd2 = new Command("cmd2", "Command 2 description");
			var cmd1Sub = new Command("sub1", "Subcommand 1 description");
			
			builder.Add("cmd1", cmd1);
			builder.Add("cmd2", cmd2);
			builder.Add("cmd1 sub1", cmd1Sub);
			
			builder.BuildTree(() => new Microsoft.Extensions.Hosting.HostBuilder().Build().Services);
			
			// Act
			var parseResult = builder.RootCommand.Parse("--help-all");
			
			// Capture console output
			var originalOut = Console.Out;
			try {
				using var writer = new StringWriter();
				Console.SetOut(writer);
				
				parseResult.Invoke();
				
				var output = writer.ToString();
				
				// Assert
				Assert.Contains("cmd1", output);
				Assert.Contains("cmd2", output);
				Assert.Contains("sub1", output);
				Assert.Contains("Command 1 description", output);
				Assert.Contains("Command 2 description", output);
				Assert.Contains("Subcommand 1 description", output);
			} finally {
				Console.SetOut(originalOut);
			}
		}

		[Fact]
		public void TestRecursiveHelpOnSubcommand() {
			// Arrange
			var builder = new CommandBuilder("Test Application");
			var cmd1 = new Command("cmd1", "Command 1");
			var sub1 = new Command("sub1", "Subcommand 1");
			var sub2 = new Command("sub2", "Subcommand 2");
			
			builder.Add("cmd1", cmd1);
			builder.Add("cmd1 sub1", sub1);
			builder.Add("cmd1 sub2", sub2);
			
			builder.BuildTree(() => new Microsoft.Extensions.Hosting.HostBuilder().Build().Services);
			
			// Act
			var parseResult = builder.RootCommand.Parse("cmd1 --help-all");
			
			// Capture console output
			var originalOut = Console.Out;
			try {
				using var writer = new StringWriter();
				Console.SetOut(writer);
				
				parseResult.Invoke();
				
				var output = writer.ToString();
				
				// Assert
				Assert.Contains("cmd1", output);
				Assert.Contains("sub1", output);
				Assert.Contains("sub2", output);
			} finally {
				Console.SetOut(originalOut);
			}
		}

		[Fact]
		public void TestRecursiveHelpOptionHasCorrectProperties() {
			var option = CommandBuilder.RecursiveHelpOption;
			
			Assert.Equal("--help-all", option.Name);
			Assert.True(option.Recursive);
			Assert.False(option.Required);
			Assert.NotNull(option.Description);
			Assert.Contains("help", option.Description.ToLower());
		}
	}
}
