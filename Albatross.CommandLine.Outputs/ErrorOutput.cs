using Newtonsoft.Json;

namespace Albatross.CommandLine.Outputs {
	public record class ErrorOutput {
		public required ErrorSource Source { get; init; }
		public string? Symbol { get; init; }
		public required string Message { get; init; }

		// if detail is actual json text, it is serialized inline as a JToken rather than a quoted string
		[JsonConverter(typeof(JsonDetailConverter))]
		public string? Detail { get; init; }
	}
}