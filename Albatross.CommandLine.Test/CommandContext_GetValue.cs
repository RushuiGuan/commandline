using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class CommandContext_GetValue {
		static CommandContext CreateContext() {
			var command = new Command("test");
			var result = command.Parse("test");
			return new CommandContext(result);
		}

		[Fact]
		public void MissingReferenceKey_ReturnsNull() {
			var context = CreateContext();
			Assert.Null(context.GetValue<string>("--key"));
		}

		[Fact]
		public void MissingStructKey_ReturnsNull() {
			var context = CreateContext();
			Assert.Null(context.GetValue<int?>("--key"));
		}
	}
}
