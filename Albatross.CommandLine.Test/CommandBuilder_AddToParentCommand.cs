using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class CommandBuilder_AddToParentCommand {
		static Task<int> GlobalHandler(ParseResult result, CancellationToken token) => Task.FromResult(0);

		[Fact]
		public void NestedKey_BuildsParentHierarchy() {
			var builder = new CommandBuilder("Test");
			var cmd = new Command("cmd1");
			builder.AddToParentCommand("parent1 parent2 cmd1", cmd, GlobalHandler);
			Assert.Equal("parent2", cmd.Parents.First().Name);
			Assert.Equal("parent1", cmd.Parents.First().Parents.First().Name);
			Assert.Equal(builder.RootCommand, cmd.Parents.First().Parents.First().Parents.First());
		}
	}
}
