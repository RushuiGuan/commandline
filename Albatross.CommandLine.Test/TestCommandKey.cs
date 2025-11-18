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
			cmd.AddCommand(cmd1);
			cmd1.AddCommand(cmd2);
			cmd2.AddCommand(cmd3);
			cmd3.AddCommand(cmd);
			Assert.Equal("1 2", cmd2.GetCommandKey());
			Assert.Equal("1 2 3", cmd3.GetCommandKey());
			Assert.Equal("1", cmd1.GetCommandKey());
		}
		
		[Fact]
		public void TestCircularReference() {
			var cmd1 = new Command("1");
			var cmd2 = new Command("2");
			var cmd3 = new Command("3");
			cmd1.AddCommand(cmd2);
			cmd2.AddCommand(cmd3);
			cmd3.AddCommand(cmd1);
			Assert.Throws<System.InvalidOperationException>(() => cmd1.GetCommandKey());
		}
	}
}
