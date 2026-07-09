using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class CommandBuilder_GetOrCreateCommand {
		static Task<int> GlobalHandler(ParseResult result, CancellationToken token) => Task.FromResult(0);

		[Fact]
		public void SingleKey_CreatesCommandUnderRoot() {
			var builder = new CommandBuilder("Test");
			builder.GetOrCreateCommand("cmd1", GlobalHandler, out var command);
			Assert.NotNull(command);
			Assert.Equal("cmd1", command.Name);
			Assert.Equal(builder.RootCommand, command.Parents.First());
		}

		[Fact]
		public void NestedKey_CreatesCommandUnderParent() {
			var builder = new CommandBuilder("Test");
			builder.GetOrCreateCommand("cmd1", GlobalHandler, out var parent);
			builder.GetOrCreateCommand("cmd1 cmd2", GlobalHandler, out var child);
			Assert.NotNull(child);
			Assert.Equal("cmd2", child.Name);
			Assert.Equal(parent, child.Parents.First());
		}
	}
}
