using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Albatross.CommandLine.Outputs {
	/// <summary>
	/// Serializes a string that may contain embedded JSON. When the value parses to a JSON object or array it is
	/// written inline as a <see cref="JToken"/> (no surrounding quotes or escaping); any other value — including a
	/// plain message or a bare JSON scalar such as <c>true</c> or <c>42</c> — is written as an ordinary JSON string
	/// so its type is preserved. On read a JSON container is captured back as its compact string form.
	/// </summary>
	public class JsonDetailConverter : JsonConverter<string?> {
		public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer) {
			if (value == null) {
				writer.WriteNull();
			} else if (TryParseContainer(value, out var token)) {
				token.WriteTo(writer);
			} else {
				writer.WriteValue(value);
			}
		}

		public override string? ReadJson(JsonReader reader, System.Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer) {
			if (reader.TokenType == JsonToken.Null) {
				return null;
			}
			var token = JToken.ReadFrom(reader);
			return token is JContainer ? token.ToString(Formatting.None) : token.Value<string>();
		}

		static bool TryParseContainer(string value, out JToken token) {
			try {
				var parsed = JToken.Parse(value);
				if (parsed is JContainer) {
					token = parsed;
					return true;
				}
			} catch (JsonReaderException) {
			}
			token = JValue.CreateNull();
			return false;
		}
	}
}
