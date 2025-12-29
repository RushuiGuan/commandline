using System.CommandLine;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class TestCommandContext {
		[Fact]
		public void TestStoringReferenceTypeValue() {
			var command = new Command("test");
			var result = command.Parse("test");
			var context = new CommandContext(result);
			var key = "--key";
			var value = "test";
			context.SetValue(key, value);
			Assert.Equal(value, context.GetReferenceValue<string>(key));
		}

		[Fact]
		public void TestStoringValueTypeValue() {
			var command = new Command("test");
			var result = command.Parse("test");
			var context = new CommandContext(result);
			var key = "--key";
			var value = 1;
			context.SetValue(key, value);
			Assert.Equal(value, context.GetStructValue<int>(key));
		}

		[Fact]
		public void TestNullReferenceValue() {
			var command = new Command("test");
			var result = command.Parse("test");
			var context = new CommandContext(result);
			var key = "--key";
			Assert.Null(context.GetReferenceValue<string>(key));
		}

		[Fact]
		public void TestNullStructValue() {
			var command = new Command("test");
			var result = command.Parse("test");
			var context = new CommandContext(result);
			var key = "--key";
			Assert.Null(context.GetStructValue<int>(key));
		}

		[Fact]
		public void TestNullException() {
			var command = new Command("test");
			var result = command.Parse("test");
			var context = new CommandContext(result);
			var key = "--key";
			Assert.Throws<ArgumentException>(() => context.SetValue<string>(key, null!));
		}
	}
}