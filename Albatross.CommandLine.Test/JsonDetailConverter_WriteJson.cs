using Albatross.CommandLine.Outputs;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class JsonDetailConverter_WriteJson {
		// Serializes an ErrorOutput through the shared CLI serializer and returns the `detail` token.
		static JToken? SerializeDetail(string? detail) {
			var error = new ErrorOutput(ErrorSource.CommandHandler, null, "message", detail);
			return JObject.FromObject(error, Outputs.Extensions.Serializer)["detail"];
		}

		[Theory]
		[InlineData("{\"code\":42,\"reason\":\"bad\"}")]
		[InlineData("[1,2,3]")]
		[InlineData("{\"a\":{\"b\":[true,null]}}")]
		[InlineData("[]")]
		[InlineData("{}")]
		public void JsonContainer_WrittenInline(string detail) {
			var token = SerializeDetail(detail);
			Assert.NotNull(token);
			Assert.IsAssignableFrom<JContainer>(token);
			Assert.True(JToken.DeepEquals(JToken.Parse(detail), token));
		}

		[Theory]
		[InlineData("file not found")]
		[InlineData("123")]
		[InlineData("true")]
		[InlineData("null")]
		[InlineData("{ not valid json")]
		[InlineData("\"already quoted\"")]
		public void NonJsonContainer_WrittenAsString(string detail) {
			var token = SerializeDetail(detail);
			Assert.NotNull(token);
			Assert.Equal(JTokenType.String, token.Type);
			Assert.Equal(detail, token.Value<string>());
		}

		[Fact]
		public void NullDetail_Omitted() {
			// The shared serializer uses NullValueHandling.Ignore, so a null Detail produces no property.
			Assert.Null(SerializeDetail(null));
		}
	}
}
