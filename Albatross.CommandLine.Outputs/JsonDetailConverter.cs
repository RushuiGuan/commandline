using System.Text.Json;
using System.Text.Json.Serialization;

namespace Albatross.CommandLine.Outputs {
	/// <summary>
	/// Serializes a string that may contain embedded JSON. When the value parses to a JSON object or array it is
	/// written inline (no surrounding quotes or escaping); any other value — including a plain message or a bare
	/// JSON scalar such as <c>true</c> or <c>42</c> — is written as an ordinary JSON string so its type is
	/// preserved. On read a JSON container is captured back as its compact string form.
	/// </summary>
	public class JsonDetailConverter : JsonConverter<string?> {
		public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options) {
			if (value == null) {
				writer.WriteNullValue();
			} else if (TryParseContainer(value, out var element)) {
				element.WriteTo(writer);
			} else {
				writer.WriteStringValue(value);
			}
		}

		public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
			using var doc = JsonDocument.ParseValue(ref reader);
			var element = doc.RootElement;
			return element.ValueKind switch {
				JsonValueKind.Null => null,
				JsonValueKind.Object or JsonValueKind.Array => JsonSerializer.Serialize(element, options),
				JsonValueKind.String => element.GetString(),
				_ => element.GetRawText(),
			};
		}

		static bool TryParseContainer(string value, out JsonElement element) {
			try {
				using var doc = JsonDocument.Parse(value);
				if (doc.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array) {
					element = doc.RootElement.Clone();
					return true;
				}
			} catch (JsonException) {
			}
			element = default;
			return false;
		}
	}
}
