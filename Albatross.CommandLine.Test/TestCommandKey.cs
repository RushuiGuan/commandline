using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class TestCommandKey {
		[Fact]
		public void TestRootCommandKey() {
			//RootCommand does have a name, but it is excluded as part of the command keys
			var root = new RootCommand();
			Assert.NotEmpty(root.Name);
			Assert.Equal(string.Empty, string.Join(string.Empty, root.GetCommandNames()));
		}
		[Fact]
		public void TestMultiLevelCommandKeys() {
			var root = new RootCommand();
			var cmd1 = new Command("1");
			var cmd2 = new Command("2");
			var cmd3 = new Command("3");
			root.Add(cmd1);
			cmd1.Add(cmd2);
			cmd2.Add(cmd3);
			Assert.Equal("1 2", string.Join(" ", cmd2.GetCommandNames()));
			Assert.Equal("1 2 3", string.Join(" ", cmd3.GetCommandNames()));
			Assert.Equal("1", string.Join(" ", cmd1.GetCommandNames()));
		}
		[Fact]
		public void TestSingleLevelCommandKey() {
			var rootCommand = new RootCommand();
			var cmd = new Command("single");
			rootCommand.Add(cmd);
			Assert.Equal("single", string.Join(" ", cmd.GetCommandNames()));
		}

		[Fact]
		public void TestNoParentCommand() {
			var cmd = new Command("orphan");
			Assert.Equal("orphan", string.Join(" ", cmd.GetCommandNames()));
		}

		[Fact]
		public void TestCircularReference() {
			var cmd1 = new Command("1");
			var cmd2 = new Command("2");
			var cmd3 = new Command("3");
			cmd1.Add(cmd2);
			cmd2.Add(cmd3);
			cmd3.Add(cmd1);
			Assert.Throws<System.InvalidOperationException>(() => cmd1.GetCommandNames());
		}
	}
}
