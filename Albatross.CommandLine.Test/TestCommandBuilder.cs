using Xunit;

namespace Albatross.CommandLine.Test {
	public class TestCommandBuilder {
		[Theory]
		[InlineData("parent child", "parent", "child")]
		[InlineData("single", "", "single")]
		[InlineData("a b c d e", "a b c d", "e")]
		[InlineData("", "", "")]
		public void TestParseCommandText(string commandText, string expectedParent, string expectedSelf) {
			var builder = new Albatross.CommandLine.CommandBuilder("Test");
			builder.ParseCommandText(commandText, out var parent, out var self);
			Assert.Equal(expectedParent, parent);
			Assert.Equal(expectedSelf, self);
		}

		[Fact]
		public void TestAddDuplicateCommand() {
			var builder = new Albatross.CommandLine.CommandBuilder("Test");
			builder.Add("cmd1", new System.CommandLine.Command("cmd1"));
			Assert.Throws<ArgumentException>(() => builder.Add("cmd1", new System.CommandLine.Command("cmd1")));
		}

		[Fact]
		public void TestAddAndRetrieveCommand() {
			var builder = new Albatross.CommandLine.CommandBuilder("Test");
			var cmd = new System.CommandLine.Command("cmd1");
			builder.Add("cmd1", cmd);
			builder.BuildTree(() => new Microsoft.Extensions.Hosting.HostBuilder().Build());
			// Assert.Single(builder.RootCommand.Children);
			Assert.Contains(cmd, builder.RootCommand.Children);
		}

		[Fact]
		public void TestGetOrCreateCommand() {
			var builder = new Albatross.CommandLine.CommandBuilder("Test");
			builder.GetOrCreateCommand("", out var command0);
			Assert.Equal(builder.RootCommand, command0);

			builder.GetOrCreateCommand("cmd1", out var command1);
			Assert.NotNull(command1);
			Assert.Equal("cmd1", command1.Name);
			Assert.Equal(builder.RootCommand, command1.Parents.First());

			builder.GetOrCreateCommand("cmd1 cmd2", out var command2);
			Assert.NotNull(command2);
			Assert.Equal("cmd2", command2.Name);
			Assert.Equal(command1, command2.Parents.First());
		}

		[Fact]
		public void TestAddToParentCommand() {
			var builder = new Albatross.CommandLine.CommandBuilder("Test");
			var cmd1 = new System.CommandLine.Command("cmd1");
			builder.AddToParentCommand("parent1 parent2 cmd1", cmd1);
			Assert.Equal("parent2", cmd1.Parents.First().Name);
			Assert.Equal("parent1", cmd1.Parents.First().Parents.First().Name);
			Assert.Equal(builder.RootCommand, cmd1.Parents.First().Parents.First().Parents.First());
		}
	}
}
