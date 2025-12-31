using Albatross.CommandLine.Annotations;
using Xunit;

namespace Albatross.CommandLine.Test.CodeGen {
	[Verb("test command class")]
	public record TestCommandClassParams {
	}

	public class TestCommandClassGeneration {
		[Fact]
		public void VerifyClassName() {
			var cmd = new TestCommandClassCommand();
		}
	}
}