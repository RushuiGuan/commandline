using Xunit;

namespace Albatross.CommandLine.Test {
	public class CommandBuilder_ParseCommandText {
		[Theory]
		[InlineData("parent child", "parent", "child")]
		[InlineData("single", "", "single")]
		[InlineData("a b c d e", "a b c d", "e")]
		[InlineData("", "", "")]
		public void ReturnsParentAndSelf(string commandText, string expectedParent, string expectedSelf) {
			CommandBuilder.ParseCommandText(commandText, out var parent, out var self);
			Assert.Equal(expectedParent, parent);
			Assert.Equal(expectedSelf, self);
		}
	}
}
