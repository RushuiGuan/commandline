using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class CommandContext_SetValue {
		static CommandContext CreateContext() {
			var command = new Command("test");
			var result = command.Parse("test");
			return new CommandContext(result);
		}

		[Fact]
		public void ReferenceType_StoresValue() {
			var context = CreateContext();
			context.SetValue("--key", "test");
			Assert.Equal("test", context.GetValue<string>("--key"));
		}

		[Fact]
		public void ValueType_StoresValue() {
			var context = CreateContext();
			context.SetValue("--key", 1);
			Assert.Equal(1, context.GetValue<int>("--key"));
		}

		[Fact]
		public void NullValue_ThrowsArgumentException() {
			var context = CreateContext();
			Assert.Throws<ArgumentException>(() => context.SetValue<string>("--key", null!));
		}
	}
}
