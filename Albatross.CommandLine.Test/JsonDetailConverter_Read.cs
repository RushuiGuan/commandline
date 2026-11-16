using System.Text.Json;
using Albatross.CommandLine.Outputs;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class JsonDetailConverter_Read {
		// Deserializes an ErrorOutput whose `detail` property is the supplied raw JSON fragment,
		// returning the resulting Detail string.
		static string? DeserializeDetail(string detailFragment) {
			var json = "{\"source\":\"CommandHandler\",\"message\":\"message\",\"detail\":" + detailFragment + "}";
			return JsonSerializer.Deserialize<ErrorOutput>(json, Outputs.Extensions.CompactSerializerOptions)!.Detail;
		}

		[Theory]
		[InlineData("{\"code\":42,\"reason\":\"bad\"}", "{\"code\":42,\"reason\":\"bad\"}")]
		[InlineData("{ \"code\" : 42 }", "{\"code\":42}")]
		[InlineData("[1, 2, 3]", "[1,2,3]")]
		[InlineData("{\n  \"a\": {\n    \"b\": [true, null]\n  }\n}", "{\"a\":{\"b\":[true,null]}}")]
		[InlineData("{}", "{}")]
		[InlineData("[]", "[]")]
		// the default encoder would escape these as <, ', + and é
		[InlineData("{\"msg\":\"a<b 'c' +1\"}", "{\"msg\":\"a<b 'c' +1\"}")]
		[InlineData("{\"name\":\"José\"}", "{\"name\":\"José\"}")]
		public void JsonContainer_CapturedAsCompactString(string detailFragment, string expected) {
			Assert.Equal(expected, DeserializeDetail(detailFragment));
		}

		[Theory]
		[InlineData("\"file not found\"", "file not found")]
		[InlineData("\"\"", "")]
		[InlineData("\"{\\\"a\\\":1}\"", "{\"a\":1}")]
		[InlineData("42", "42")]
		[InlineData("1.50", "1.50")]
		[InlineData("true", "true")]
		[InlineData("false", "false")]
		public void JsonScalar_CapturedAsString(string detailFragment, string expected) {
			Assert.Equal(expected, DeserializeDetail(detailFragment));
		}

		[Fact]
		public void NullDetail_ReturnsNull() {
			Assert.Null(DeserializeDetail("null"));
		}

		[Theory]
		[InlineData("{\"code\":42,\"reason\":\"bad\"}")]
		[InlineData("[{\"a\":1},{\"b\":[]}]")]
		[InlineData("{\"msg\":\"a<b 'c' +1\"}")]
		[InlineData("file not found")]
		[InlineData("123")]
		public void WrittenDetail_RoundTripsUnchanged(string detail) {
			var error = new ErrorOutput(ErrorSource.CommandHandler, null, "message", detail);
			var json = JsonSerializer.Serialize(error, Outputs.Extensions.CompactSerializerOptions);
			Assert.Equal(detail, JsonSerializer.Deserialize<ErrorOutput>(json, Outputs.Extensions.CompactSerializerOptions)!.Detail);
		}
	}
}
