using System.Text.Json;
using System.Text.Json.Nodes;
using Albatross.CommandLine.Outputs;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class JsonDetailConverter_Write {
		static ErrorOutput MakeError(string? detail) => new ErrorOutput(ErrorSource.CommandHandler, null, "message", detail);

		// Serializes an ErrorOutput through the shared CLI serializer options and returns it as a node.
		static JsonObject SerializeError(string? detail)
			=> JsonSerializer.SerializeToNode(MakeError(detail), Outputs.Extensions.CompactSerializerOptions)!.AsObject();

		[Theory]
		[InlineData("{\"code\":42,\"reason\":\"bad\"}")]
		[InlineData("[1,2,3]")]
		[InlineData("{\"a\":{\"b\":[true,null]}}")]
		[InlineData("[]")]
		[InlineData("{}")]
		[InlineData("  {\"a\":1}\n")]
		[InlineData("{\n  \"a\": 1\n}")]
		public void JsonContainer_WrittenInline(string detail) {
			var node = SerializeError(detail)["detail"];
			Assert.True(node is JsonObject or JsonArray);
			Assert.True(JsonNode.DeepEquals(JsonNode.Parse(detail), node));
		}

		[Theory]
		[InlineData("file not found")]
		[InlineData("")]
		[InlineData("123")]
		[InlineData("true")]
		[InlineData("null")]
		[InlineData("{ not valid json")]
		[InlineData("{} trailing")]
		[InlineData("{\"a\":1}{\"b\":2}")]
		[InlineData("\"already quoted\"")]
		[InlineData("line1\nline2")]
		public void NonJsonContainer_WrittenAsString(string detail) {
			var node = SerializeError(detail)["detail"];
			Assert.NotNull(node);
			Assert.Equal(JsonValueKind.String, node.GetValueKind());
			Assert.Equal(detail, node.GetValue<string>());
		}

		[Theory]
		[InlineData("{\n  \"code\": 42\n}", "\"detail\":{\"code\":42}")]
		[InlineData("{\"msg\":\"a<b 'c'\"}", "\"detail\":{\"msg\":\"a<b 'c'\"}")]
		[InlineData("a<b 'c'", "\"detail\":\"a<b 'c'\"")]
		public void Detail_WrittenUnescapedAndCompact(string detail, string expected) {
			var json = JsonSerializer.Serialize(MakeError(detail), Outputs.Extensions.CompactSerializerOptions);
			Assert.Contains(expected, json);
		}

		[Fact]
		public void NullDetail_Omitted() {
			// The shared options ignore null properties, so a null Detail produces no property.
			Assert.False(SerializeError(null).ContainsKey("detail"));
		}
	}
}
