using System.Text.Json.Serialization;

namespace Albatross.CommandLine.Outputs {
	public record class CommandOutput {
		public required string Command { get; init; }
		public string? Message { get; set; }
		public IReadOnlyCollection<ErrorOutput>? Errors { get; set; }
		public required int ExitCode { get; init; }
		public string? LogFolder { get; set; }
	}

	public record class CommandOutput<T> : CommandOutput {
		[JsonPropertyOrder(100)]
		public T? Data { get; set; }
	}
}
