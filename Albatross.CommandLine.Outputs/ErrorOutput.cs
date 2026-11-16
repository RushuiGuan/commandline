using System.Text.Json.Serialization;

namespace Albatross.CommandLine.Outputs {
	public class ErrorOutput {
		public ErrorSource Source { get; }
		public string? Symbol { get; }
		public string? Message { get; }

		// if detail is actual json text, it is serialized inline rather than as a quoted string
		[JsonConverter(typeof(JsonDetailConverter))]
		public string? Detail { get; }


		[JsonConstructor]
		public ErrorOutput(ErrorSource source, string? symbol, string? message, string? detail) {
			this.Source = source;
			if (source != ErrorSource.CommandHandler) {
				Symbol = symbol;
			}
			this.Message = message;
			this.Detail = detail;
		}

		public ErrorOutput(Error error) : this(error.Source, error.Symbol, error.Message, error.Exception?.Message) { }
	}
}
