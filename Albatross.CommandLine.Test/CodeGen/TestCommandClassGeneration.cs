using Albatross.CommandLine.Annotations;
using Xunit;

namespace Albatross.CommandLine.Test.CodeGen {
	[Verb("test command class")]
	public record TestCommandClassParams {
		public string? IgnoredProperty { get; set; }
	}

	public class TestCommandClassGeneration {
		
		[Fact]
		public void VerifyClassName() {
			var cmd = new TestCommandClassCommand();
			Assert.Equal("class", cmd.Name);
		}

		[Fact]
		public void VerifyIgnoreProperty() {
			var type = typeof(TestCommandClassCommand);
		}
	}
}