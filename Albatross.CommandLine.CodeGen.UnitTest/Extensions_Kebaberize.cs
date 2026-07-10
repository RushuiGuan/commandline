using Xunit;

namespace Albatross.CommandLine.CodeGen.UnitTest
{
    public class StringExtensions_Kebaberize
    {
		[Theory]
		[InlineData("BBYellowKey", "bb-yellow-key")]
		[InlineData("AAAA", "aaaa")]
		[InlineData("AAAa", "aa-aa")]
		[InlineData("AaBbCcDd", "aa-bb-cc-dd")]
		[InlineData("AAbbCCdd", "a-abb-c-cdd")]
		[InlineData("abc", "abc")]
		[InlineData("a-b-c", "a-b-c")]
		[InlineData("Ab-Cd-Ef", "ab-cd-ef")]
		[InlineData(" abc ", "-abc-")]
		[InlineData("  abc  ", "-abc-")]
		[InlineData("  Abc  ", "-abc-")]
		[InlineData("SomePropertyName", "some-property-name")]
		[InlineData("somePropertyName", "some-property-name")]
		[InlineData("Some property name", "some-property-name")]
		[InlineData("Some  property  name", "some-property-name")]
		[InlineData("some_property_name", "some-property-name")]
		[InlineData("some__property__name", "some-property-name")]
		[InlineData("Some_Property_Name", "some-property-name")]
		[InlineData("123", "123")]
		[InlineData("A1A2A3", "a1-a2-a3")]

		[InlineData("CUSIP", "cusip")]
		[InlineData("CUSIPCode", "cusip-code")]
		[InlineData("MyCUSIPCode", "my-cusip-code")]
		[InlineData("CusipCode", "cusip-code")]

		[InlineData("", "")]
		[InlineData(" ", "-")]
		[InlineData("  ", "-")]
		[InlineData("--", "-")]
		[InlineData("__", "-")]
		[InlineData(" _-", "-")]
		public void RunKebaberize(string input, string expected) {
			var output = input.Kebaberize();
			Assert.Equal(expected, output);
		}
	}
}
