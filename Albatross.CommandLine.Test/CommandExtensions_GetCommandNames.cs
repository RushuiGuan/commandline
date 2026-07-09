using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class CommandExtensions_GetCommandNames {
		[Fact]
		public void RootCommand_ReturnsEmpty() {
			// RootCommand has a name, but it is excluded from the command names.
			var root = new RootCommand();
			Assert.NotEmpty(root.Name);
			Assert.Equal(string.Empty, string.Join(string.Empty, root.GetCommandNames()));
		}

		[Fact]
		public void SingleLevelCommand_ReturnsName() {
			var root = new RootCommand();
			var cmd = new Command("single");
			root.Add(cmd);
			Assert.Equal("single", string.Join(" ", cmd.GetCommandNames()));
		}

		[Fact]
		public void MultiLevelCommand_ReturnsFullPath() {
			var root = new RootCommand();
			var cmd1 = new Command("1");
			var cmd2 = new Command("2");
			var cmd3 = new Command("3");
			root.Add(cmd1);
			cmd1.Add(cmd2);
			cmd2.Add(cmd3);
			Assert.Equal("1", string.Join(" ", cmd1.GetCommandNames()));
			Assert.Equal("1 2", string.Join(" ", cmd2.GetCommandNames()));
			Assert.Equal("1 2 3", string.Join(" ", cmd3.GetCommandNames()));
		}

		[Fact]
		public void NoParentCommand_ReturnsOwnName() {
			var cmd = new Command("orphan");
			Assert.Equal("orphan", string.Join(" ", cmd.GetCommandNames()));
		}

		[Fact]
		public void CircularReference_ThrowsInvalidOperationException() {
			var cmd1 = new Command("1");
			var cmd2 = new Command("2");
			var cmd3 = new Command("3");
			cmd1.Add(cmd2);
			cmd2.Add(cmd3);
			cmd3.Add(cmd1);
			Assert.Throws<InvalidOperationException>(() => cmd1.GetCommandNames());
		}
	}
}
