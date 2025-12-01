using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class TestCommandKey {
		[Fact]
		public void TestGetKey() {
			var cmd = new RootCommand();
			var cmd1 = new Command("1");
			var cmd2 = new Command("2");
			var cmd3 = new Command("3");
			cmd.Add(cmd1);
			cmd1.Add(cmd2);
			cmd2.Add(cmd3);
			cmd3.Add(cmd);
			Assert.Equal("1 2", string.Join(" ", cmd2.GetCommandNames()));
			Assert.Equal("1 2 3", string.Join(" ", cmd3.GetCommandNames()));
			Assert.Equal("1", string.Join(" ", cmd1.GetCommandNames()));
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
