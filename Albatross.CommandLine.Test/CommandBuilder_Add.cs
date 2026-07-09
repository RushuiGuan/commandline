using System.CommandLine;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class CommandBuilder_Add {
		[Fact]
		public void DuplicateKey_ThrowsArgumentException() {
			var builder = new CommandBuilder("Test");
			builder.Add("cmd1", new Command("cmd1"));
			Assert.Throws<ArgumentException>(() => builder.Add("cmd1", new Command("cmd1")));
		}

		[Fact]
		public void AddedCommand_IsLinkedToRoot() {
			var builder = new CommandBuilder("Test");
			var cmd = new Command("cmd1");
			builder.Add("cmd1", cmd);
			builder.BuildTree(() => new HostBuilder().Build().Services);
			Assert.Contains(cmd, builder.RootCommand.Children);
		}
	}
}
