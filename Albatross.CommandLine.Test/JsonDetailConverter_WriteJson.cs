using System.Text.Json;
using System.Text.Json.Nodes;
using Albatross.CommandLine.Outputs;
using Xunit;

namespace Albatross.CommandLine.Test {
	public class JsonDetailConverter_WriteJson {
		// Serializes an ErrorOutput through the shared CLI serializer options and returns the `detail` node.
		static JsonObject SerializeError(string? detail) {
			var error = new ErrorOutput(ErrorSource.CommandHandler, null, "message", detail);
			return JsonSerializer.SerializeToNode(error, Outputs.Extensions.SerializerOptions)!.AsObject();
		}

		[Theory]
		[InlineData("{\"code\":42,\"reason\":\"bad\"}")]
		[InlineData("[1,2,3]")]
		[InlineData("{\"a\":{\"b\":[true,null]}}")]
		[InlineData("[]")]
		[InlineData("{}")]
		public void JsonContainer_WrittenInline(string detail) {
			var node = SerializeError(detail)["detail"];
			Assert.True(node is JsonObject or JsonArray);
			Assert.True(JsonNode.DeepEquals(JsonNode.Parse(detail), node));
		}

		[Theory]
		[InlineData("file not found")]
		[InlineData("123")]
		[InlineData("true")]
		[InlineData("null")]
		[InlineData("{ not valid json")]
		[InlineData("\"already quoted\"")]
		public void NonJsonContainer_WrittenAsString(string detail) {
			var node = SerializeError(detail)["detail"];
			Assert.NotNull(node);
			Assert.Equal(JsonValueKind.String, node.GetValueKind());
			Assert.Equal(detail, node.GetValue<string>());
		}

		[Fact]
		public void NullDetail_Omitted() {
			// The shared options ignore null properties, so a null Detail produces no property.
			Assert.False(SerializeError(null).ContainsKey("detail"));
		}
	}
}
