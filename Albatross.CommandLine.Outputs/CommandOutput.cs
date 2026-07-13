using Newtonsoft.Json;

namespace Albatross.CommandLine.Outputs {
	public record class CommandOutput {
		public required string Command { get; init; }
		public string? Message { get; set; }
		public ErrorOutput[] Errors { get; set; } = [];
		public required int ExitCode { get; init; }
		public string? LogFolder { get; set; }
	}

	public record class ErrorOutput {
		public required ErrorSource Source { get; init; }
		public string? Symbol { get; init; }
		public required string Message { get; init; }
		public string? Detail { get; init; }
	}

	public record class CommandOutput<T> : CommandOutput {
		// Order pushes Data after the inherited members, which Newtonsoft would otherwise emit last
		// (it serializes a derived type's own members first).
		[JsonProperty(Order = 100)]
		public T? Data { get; set; }
	}
}