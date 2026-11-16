using System.Text.Json;
using Albatross.CommandLine.Outputs;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class JsonDetailConverter_ReadJson {
		// Deserializes an ErrorOutput whose `detail` property is the supplied raw JSON fragment,
		// returning the resulting Detail string.
		static string? DeserializeDetail(string detailFragment) {
			var json = "{\"source\":\"CommandHandler\",\"message\":\"message\",\"detail\":" + detailFragment + "}";
			return JsonSerializer.Deserialize<ErrorOutput>(json, Outputs.Extensions.SerializerOptions)!.Detail;
		}

		[Theory]
		[InlineData("{\"code\":42,\"reason\":\"bad\"}", "{\"code\":42,\"reason\":\"bad\"}")]
		[InlineData("{ \"code\" : 42 }", "{\"code\":42}")]
		[InlineData("[1, 2, 3]", "[1,2,3]")]
		[InlineData("{}", "{}")]
		public void JsonContainer_CapturedAsCompactString(string detailFragment, string expected) {
			Assert.Equal(expected, DeserializeDetail(detailFragment));
		}

		[Theory]
		[InlineData("\"file not found\"", "file not found")]
		[InlineData("\"\"", "")]
		[InlineData("42", "42")]
		public void JsonScalar_CapturedAsString(string detailFragment, string expected) {
			Assert.Equal(expected, DeserializeDetail(detailFragment));
		}

		[Fact]
		public void NullDetail_ReturnsNull() {
			Assert.Null(DeserializeDetail("null"));
		}
	}
}
